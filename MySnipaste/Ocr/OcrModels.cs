using System.Text.Json.Serialization;

namespace OcrSnap.Ocr
{
    public class OcrResult
    {
        [JsonPropertyName("markdown")]
        public string Markdown { get; set; } = "";

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("processTimeMs")]
        public int ProcessTimeMs { get; set; }

        [JsonPropertyName("regions")]
        public OcrRegion[] Regions { get; set; } = [];
    }

    public class OcrRegion
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("boundingBox")]
        public BoundingBox? BoundingBox { get; set; }
    }

    public class BoundingBox
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("width")]
        public double Width { get; set; }

        [JsonPropertyName("height")]
        public double Height { get; set; }
    }
}
