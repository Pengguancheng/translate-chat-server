using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using TranslateChat.Applibs;
using TranslateChat.Domain.Model;

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
    chatRooms[lang] = new ChatRoom(lang);
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
    if (!chatRooms.ContainsKey(user.Language))
    {
        logger.Warn($"No chat room found for language {user.Language}");
        return;
    }

    var welcomeMessage = $"Welcome, {user.Name}! You are now connected to the chat.";
    try
    {
        await chatRooms[user.Language].AddUser(user);
        var exTasks = new List<Task<Exception?>>();
        foreach (var room in chatRooms.Values)
        {
            var msg = new ChatMessage(user, welcomeMessage);
            exTasks.Add(room.BroadcastMessage(msg));
        }

        await Task.WhenAll(exTasks);
        var exList = exTasks.Select(x => x.Result).Where(x => x != null).ToList();
        if (exList.Count > 0)
        {
            logger.Error(
                $"Error broadcasting welcome message to chat rooms: {string.Join(", ", exList.Select(x => x.Message))}");
        }

        while (!receiveResult.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);


            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }
    }
    finally
    {
        await chatRooms[user.Language].RemoveUser(user.Id);
    }
}