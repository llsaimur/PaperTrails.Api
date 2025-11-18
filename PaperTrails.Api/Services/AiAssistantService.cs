using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PaperTrails.Api.Services
{
    public class AiAssistantService : IAiAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AiAssistantService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> AskAsync(string userId, string documentText, string query)
        {
            var prompt =
                $"You are a helpful AI assistant that answers questions only using the provided document.\n" +
                "Be concise, factual, and respond in plain English.\n\n" +
                $"Document:\n\"\"\"{documentText}\"\"\"\n\n" +
                $"User question:\n{query}\n\n" +
                "If the document does not contain enough information, say: " +
                "'I could not find that information in this document.'";


            var payload = new
            {
                model = "mistralai/mistral-7b-instruct",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful document assistant." },
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["OpenRouter:ApiKey"]}");

            try
            {
                var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                string? contentText = null;

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var text))
                {
                    contentText = text.GetString();
                }

                return string.IsNullOrWhiteSpace(contentText)
                    ? "[No meaningful response from model]"
                    : contentText.Trim();
            }
            catch (HttpRequestException ex)
            {
                return $"[AI service unavailable: {ex.Message}]";
            }
            catch (JsonException ex)
            {
                return $"[Error parsing AI response: {ex.Message}]";
            }
            catch (Exception ex)
            {
                return $"[Unexpected error: {ex.Message}]";
            }
        }
    }
}
