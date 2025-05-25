using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranslateChat.Domain.Model;

public class User
{
    public User(string id, string name, string language, WebSocket webSocket)
    {
        Id = id;
        Name = name;
        Language = language;
        WebSocket = webSocket;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Language { get; set; }
    [JsonIgnore]
    public WebSocket WebSocket { get; set; }

    public async Task SendMessage(ChatMessage message)
    {
        if (WebSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not open.");
        }

        var msgStr = message.ToJsonString();

        var buffer = Encoding.UTF8.GetBytes(msgStr);
        var segment = new ArraySegment<byte>(buffer);

        await WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task CloseWebSocket(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing the connection")
    {
        if (WebSocket.State == WebSocketState.Open)
        {
            try
            {
                await WebSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as appropriate for your application
                Console.WriteLine($"Error closing WebSocket for user {Id}: {ex.Message}");
            }
        }
    }
}