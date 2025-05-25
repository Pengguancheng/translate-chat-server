using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TranslateChat.Domain.Model
{
    public class TranslateResponse
    {
        [JsonPropertyName("alternatives")]
        public List<string> Alternatives { get; set; }

        [JsonPropertyName("detectedLanguage")]
        public DetectedLanguage DetectedLanguage { get; set; }

        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }

    public class DetectedLanguage
    {
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }
    }
}