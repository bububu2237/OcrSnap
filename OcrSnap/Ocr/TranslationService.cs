using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OcrSnap.Ocr
{
    public static class TranslationService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

        public static bool IsConfigured => !string.IsNullOrWhiteSpace(App.Settings.TranslateApiUrl);

        public static async Task<string> TranslateAsync(string text, string targetLanguageLabel)
        {
            var s = App.Settings;
            var payload = JsonSerializer.Serialize(new
            {
                model = s.TranslateModel,
                messages = new object[]
                {
                    new { role = "system", content = $"你是專業翻譯，請將使用者提供的文字翻譯成{targetLanguageLabel}，只輸出翻譯結果，不要加任何說明或前後綴。" },
                    new { role = "user", content = text }
                }
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, s.TranslateApiUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(s.TranslateApiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.TranslateApiKey);

            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message")
                      .GetProperty("content").GetString()?.Trim() ?? "";
        }
    }
}
