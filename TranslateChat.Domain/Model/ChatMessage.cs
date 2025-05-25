using System;
using System.Collections.Generic;

namespace TranslateChat.Domain.Model;

public class ChatMessage
{
    public string Id { get; private set; }
    public User Sender { get; set; }
    public string OriginalContent { get; set; }
    public string OriginalLanguage { get; set; }
    public string TranslatedLanguage { get; set; }
    public string TranslatedContent { get; set; }
    public DateTime Timestamp { get; private set; }

    public ChatMessage(User sender, string originalContent)
    {
        Id = Guid.NewGuid().ToString();
        OriginalContent = originalContent;
        OriginalLanguage = sender.Language;
        Sender = sender;
        Timestamp = DateTime.UtcNow;
    }
}