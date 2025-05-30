using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using TranslateChat.Applibs;
using TranslateChat.Domain.Model;
using TranslateChat.Model;

// See https://aka.ms/new-console-template for more information

NLog.LogManager.Configuration = new NLogLoggingConfiguration(ConfigHelper.Config.GetSection("NLog"));

Console.WriteLine($"Start Config for {ConfigHelper.Env}");

ThreadPool.SetMinThreads(20, 20);
ThreadPool.GetMinThreads(out int workThreads, out int ioThreads);

LogManager.GetCurrentClassLogger().Info($"application start {ConfigHelper.Env}");

var builder = WebApplication.CreateBuilder(args);

// 從配置中讀取端口，如果沒有配置，則使用默認端口5000
var port = builder.Configuration.GetValue<int>("Port", 8080);

// 設置 Kestrel 服務器選項
builder.WebHost.UseKestrel(options => { options.ListenAnyIP(port); });

var chatRooms = new ConcurrentDictionary<string, ChatRoom>();
foreach (var lang in ConfigHelper.ChatLanguages)
{
    chatRooms[lang] = new ChatRoom(lang, ConfigHelper.TranslatorUrl);
}

builder.Host.UseNLog();
// Autofac Add services to the container.
{
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Host.ConfigureContainer<ContainerBuilder>((hostContext, containerBuilder) =>
    {
        var asm = Assembly.GetExecutingAssembly();
    });
}

var app = builder.Build();

// 在應用啟動時輸出監聽的端口信息
app.Lifetime.ApplicationStarted.Register(() =>
{
    LogManager.GetCurrentClassLogger().Info($"Application is listening on port {port}");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    LogManager.GetCurrentClassLogger().Info("Application is shutting down");
    var tasks = new List<Task>();
    foreach (var room in chatRooms.Values)
    {
        tasks.Add(room.ClearUsers());
    }
    Task.WaitAll(tasks.ToArray());
});


app.UseWebSockets();

app.Map("/ws", async (context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        // Extract user information from query parameters
        var userId = context.Request.Query["userId"].ToString();
        var userName = context.Request.Query["userName"].ToString();
        var userLanguage = context.Request.Query["language"].ToString();

        // Validate user information
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userLanguage))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(
                "Missing user information. Please provide userId, userName, and language.");
            return;
        }

        var lifetimeScope = context.RequestServices.GetRequiredService<ILifetimeScope>();

        using var ws = await context.WebSockets.AcceptWebSocketAsync();

        var user = new User(userId, userName, userLanguage, ws);
        LogManager.GetCurrentClassLogger()
            .Info($"Started WebSocket connection for user {JsonConvert.SerializeObject(user)}");
        await HandleWebSocketConnection(user, ws, lifetimeScope);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();

async Task HandleWebSocketConnection(User user, WebSocket webSocket, ILifetimeScope lifetimeScope)
{
    var buffer = new byte[1024 * 4];
    var receiveResult = await webSocket.ReceiveAsync(
        new ArraySegment<byte>(buffer), CancellationToken.None);
    await using var scope = lifetimeScope.BeginLifetimeScope();
    var logger = LogManager.GetCurrentClassLogger();
    if (!chatRooms.TryGetValue(user.Language, out var curRoom))
    {
        logger.Warn($"No chat room found for language {user.Language}");
        return;
    }

    try
    {
        await curRoom.AddUser(user);

        while (!receiveResult.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

            var ex = await curRoom.BroadcastMessage(new ChatMessage(user, message));
            if (ex != null)
            {
                logger.Error($"Error broadcasting message: {ex.Message}");
            }

            foreach (var chatRoom in chatRooms)
            {
                if (chatRoom.Key != user.Language)
                {
                    var translateEx = await chatRoom.Value.BroadcastTranslatedMessage(new ChatMessage(user, message));
                    if (translateEx != null)
                    {
                        logger.Error($"Error broadcasting translated message: {translateEx.Message}");
                    }
                }
            }

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }
    }
    finally
    {
        await chatRooms[user.Language].RemoveUser(user.Id);
    }
}