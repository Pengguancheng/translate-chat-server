using System;
using System.Collections.Generic;

namespace TranslateChat.Domain.Model;

public class ChatMessage
{
    public string Id { get; private set; }
    public string ChatRoomId { get; set; }
    public User Sender { get; set; }
    public string OriginalContent { get; set; }
    public string OriginalLanguage { get; set; }
    public string TranslatedLanguage { get; set; }
    public string TranslatedContent { get; set; }
    public DateTime Timestamp { get; private set; }

    public ChatMessage( User sender,string chatRoomId,  string originalContent, string originalLanguage, string translatedLanguage)
    {
        Id = Guid.NewGuid().ToString();
        ChatRoomId = chatRoomId;
        OriginalContent = originalContent;
        OriginalLanguage = originalLanguage;
        TranslatedLanguage = translatedLanguage;
        Sender = sender;
        Timestamp = DateTime.UtcNow;
    }
}