using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TranslateChat.Domain.Model;

public class ChatRoom
{
    public string Language { get; set; }
    private readonly ConcurrentDictionary<string, User> userDict;

    public ChatRoom(string language)
    {
        Language = language;
        userDict = new ConcurrentDictionary<string, User>();
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
}