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

// See https://aka.ms/new-console-template for more information

NLog.LogManager.Configuration = new NLogLoggingConfiguration(ConfigHelper.Config.GetSection("NLog"));

Console.WriteLine($"Start Config for {ConfigHelper.Env}");

ThreadPool.SetMinThreads(20, 20);
ThreadPool.GetMinThreads(out int workThreads, out int ioThreads);

LogManager.GetCurrentClassLogger().Info($"application start {ConfigHelper.Env}");

var builder = WebApplication.CreateBuilder(args);

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

var chatClients = new ConcurrentDictionary<string, WebSocket>();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var clientId = Guid.NewGuid().ToString();
        chatClients.TryAdd(clientId, ws);

        await BroadcastMessage($"User {clientId} has joined the chat.");

        await HandleWebSocketConnection(clientId, ws);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();

async Task HandleWebSocketConnection(string clientId, WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    var receiveResult = await webSocket.ReceiveAsync(
        new ArraySegment<byte>(buffer), CancellationToken.None);

    try
    {
        while (!receiveResult.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            await BroadcastMessage($"User {clientId}: {message}");

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }
    }
    finally
    {
        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
        chatClients.TryRemove(clientId, out _);
        await BroadcastMessage($"User {clientId} has left the chat.");
    }
}

async Task BroadcastMessage(string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    foreach (var client in chatClients)
    {
        if (client.Value.State == WebSocketState.Open)
        {
            await client.Value.SendAsync(
                new ArraySegment<byte>(bytes, 0, bytes.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}
