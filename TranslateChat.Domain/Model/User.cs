namespace TranslateChat.Domain.Model;

public class User
{
    public User(string id, string name, string language)
    {
        Id = id;
        Name = name;
        Language = language;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Language { get; set; }
}