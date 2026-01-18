using Microsoft.AspNetCore.Mvc;
using ChatBotApi.Models;
using System.Text;
using System.Text.Json;

namespace ChatBotApi.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30) // cloud = fast
            };
        }

        [HttpPost]
        public async Task<IActionResult> Ask(ChatRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("Gemini API key missing");

            // 🔹 CHANGE THIS after Step 1
            var modelName = "models/gemini-2.5-flash"; // example

            var geminiRequest = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = request.Message }
                }
            }
        }
            };

            var json = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1/{modelName}:generateContent?key={apiKey}",
                content);

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);

            if (doc.RootElement.TryGetProperty("error", out var error))
                return BadRequest(error.GetProperty("message").GetString());

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
                return BadRequest("No response from Gemini");

            var reply = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return Ok(reply);
        }




    }
}
