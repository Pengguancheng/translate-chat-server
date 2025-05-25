using System.Text.Json.Serialization;

namespace TranslateChat.Domain.Model
{
    public class TranslateRequest
    {
        [JsonPropertyName("q")]
        public string Text { get; set; }

        [JsonPropertyName("source")]
        public string SourceLanguage { get; set; }

        [JsonPropertyName("target")]
        public string TargetLanguage { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("alternatives")]
        public int Alternatives { get; set; }

        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; }

        public TranslateRequest(string text, string sourceLanguage, string targetLanguage, int alternatives = 1)
        {
            Text = text;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
            Format = "text";
            Alternatives = alternatives;
            ApiKey = "";
        }
    }
}