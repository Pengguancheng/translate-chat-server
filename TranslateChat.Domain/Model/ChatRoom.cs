using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TranslateChat.Domain.Model;

public class ChatRoom
{
    public string Language { get; set; }
    public string TranslateUrl { get; set; }
    private readonly ConcurrentDictionary<string, User> userDict;
    private readonly HttpClient _httpClient;

    public ChatRoom(string language, string translateUrl)
    {
        Language = language;
        TranslateUrl = translateUrl;
        userDict = new ConcurrentDictionary<string, User>();
        _httpClient = new HttpClient();
    }

    public IReadOnlyCollection<User> Users => userDict.Values.ToList().AsReadOnly();

    public async Task AddUser(User user)
    {
        userDict.TryAdd(user.Id, user);
    }

    public async Task<bool> RemoveUser(string userId)
    {
        if (userDict.TryRemove(userId, out var user))
        {
            await user.CloseWebSocket();
            return true;
        }

        return false;
    }

    public User? GetUser(string userId)
    {
        userDict.TryGetValue(userId, out var user);
        return user;
    }

    public async Task<Exception?> BroadcastMessage(ChatMessage msg)
    {
        try
        {
            var tasks = new List<Task>();
            foreach (var user in userDict.Values)
            {
                tasks.Add(user.SendMessage(msg));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    public async Task<Exception?> BroadcastTranslatedMessage(ChatMessage originalMsg)
    {
        try
        {
            var request =
                new TranslateRequest(originalMsg.OriginalContent, originalMsg.OriginalLanguage, this.Language);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{TranslateUrl}/translate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var translatedResponse = JsonSerializer.Deserialize<TranslateResponse>(responseBody)!;

            var msg = new ChatMessage(originalMsg.Sender, originalMsg.OriginalContent)
            {
                OriginalLanguage = translatedResponse.DetectedLanguage.Language,
                TranslatedLanguage = this.Language,
                TranslatedContent = translatedResponse.TranslatedText
            };
            return await BroadcastMessage(msg);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}