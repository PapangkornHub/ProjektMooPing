using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjektMooPing.Services
{
    /// <summary>
    /// เรียก Google Gemini API เพื่อขอคำอธิบายเพิ่มเติมของคำตอบใน Discover Quiz
    /// สมัคร Key ฟรีที่ https://aistudio.google.com/app/apikey
    /// </summary>
    public static class AiExplainService
    {
        // API Key อ่านจาก AiSecrets.cs ซึ่งอยู่ใน .gitignore (ไม่ commit ลง git)
        private static string ApiKey => AiSecrets.GeminiApiKey;

        // gemini-2.5-flash-lite = quota สูง เร็ว เหมาะกับ trivia สั้นๆ ✅
        // gemini-2.5-flash      = คุณภาพสูงกว่า แต่ quota น้อยกว่า
        private const string Model = "gemini-2.5-flash-lite";
        private static readonly string Endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Cache คำอธิบายต่อ (questionId, isThai) เพื่อประหยัด quota
        private static readonly Dictionary<string, string> _cache = new();

        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != "YOUR_GEMINI_API_KEY_HERE";

        public static async Task<string> ExplainAsync(
            int questionId, string question, string correctAnswer, bool isThai, CancellationToken ct = default)
        {
            string cacheKey = $"{questionId}_{(isThai ? "th" : "en")}";
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            if (!IsConfigured)
                return isThai
                    ? "(ยังไม่ได้ตั้งค่า API Key ของ Gemini)"
                    : "(Gemini API Key not configured)";

            string prompt = isThai
                ? $"อธิบายแบบสั้น 2-3 ประโยค เพื่อให้ผู้เล่นเกมเข้าใจว่าทำไม \"{correctAnswer}\" จึงเป็นคำตอบที่ถูกต้องของคำถาม: \"{question}\" ตอบเป็นภาษาไทยเท่านั้น เน้นความรู้รอบตัว"
                : $"In 2-3 short sentences, explain why \"{correctAnswer}\" is the correct answer to: \"{question}\". Focus on trivia. Answer in English only.";

            try
            {
                var body = new GeminiRequest
                {
                    Contents = new[]
                    {
                        new GeminiContent
                        {
                            Parts = new[] { new GeminiPart { Text = prompt } }
                        }
                    },
                    GenerationConfig = new GeminiGenConfig
                    {
                        // ปิด "thinking" ของ 2.5 → ตอบเร็ว ประหยัด token 10+ เท่า
                        ThinkingConfig = new GeminiThinkingConfig { ThinkingBudget = 0 }
                    }
                };

                HttpResponseMessage resp = null;
                int[] backoffMs = { 0, 1500, 4000 }; // ลอง 3 ครั้ง: ทันที, 1.5s, 4s
                for (int attempt = 0; attempt < backoffMs.Length; attempt++)
                {
                    if (backoffMs[attempt] > 0)
                        await Task.Delay(backoffMs[attempt], ct);

                    resp?.Dispose();
                    resp = await _http.PostAsJsonAsync(Endpoint, body, ct);

                    // 429 = rate limit → retry; อื่นๆ ออกเลย
                    if ((int)resp.StatusCode != 429) break;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    int code = (int)resp.StatusCode;
                    resp.Dispose();
                    return isThai
                        ? (code == 429
                            ? "(AI ใช้งานหนักเกินไป กรุณารอสักครู่)"
                            : $"(เชื่อมต่อ AI ไม่สำเร็จ: {code})")
                        : (code == 429
                            ? "(AI rate limit exceeded — try again later)"
                            : $"(AI request failed: {code})");
                }

                var json = await resp.Content.ReadAsStringAsync(ct);
                resp.Dispose();
                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(text))
                    _cache[cacheKey] = text;

                return string.IsNullOrWhiteSpace(text)
                    ? (isThai ? "(ไม่ได้รับคำตอบจาก AI)" : "(No response from AI)")
                    : text;
            }
            catch (TaskCanceledException)
            {
                return isThai ? "(หมดเวลารอ AI ตอบกลับ)" : "(AI response timed out)";
            }
            catch (Exception ex)
            {
                return isThai
                    ? $"(เกิดข้อผิดพลาด: {ex.Message})"
                    : $"(Error: {ex.Message})";
            }
        }

        // ─── Gemini DTOs ────────────────────────────────────────────
        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();

            [JsonPropertyName("generationConfig")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public GeminiGenConfig GenerationConfig { get; set; }
        }
        private class GeminiGenConfig
        {
            [JsonPropertyName("thinkingConfig")]
            public GeminiThinkingConfig ThinkingConfig { get; set; }
        }
        private class GeminiThinkingConfig
        {
            [JsonPropertyName("thinkingBudget")]
            public int ThinkingBudget { get; set; }
        }
        private class GeminiContent
        {
            [JsonPropertyName("parts")]
            public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
        }
        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }
    }
}
