using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EventTicketSystem.Web.Models.Configuration;
using Microsoft.Extensions.Options;

namespace EventTicketSystem.Web.Services;

public class GroqService(
    IHttpClientFactory httpFactory,
    IOptions<AISettings> aiOptions,
    ILogger<GroqService> logger)
{
    private readonly AISettings _settings = aiOptions.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task<string?> ChatAsync(
        string? systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Groq API key chưa được cấu hình (AI:ApiKey trống).");
            return null;
        }

        logger.LogInformation("Calling Groq → {Endpoint} model={Model}", _settings.Endpoint, _settings.Model);

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model       = _settings.Model,
            messages    = messages,
            temperature = 0.7,
            max_tokens  = 1024
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var http = httpFactory.CreateClient("groq");
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await http.PostAsync(_settings.Endpoint, content, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Groq API lỗi {StatusCode} ({Reason}).\nBody:\n{Body}",
                    (int)response.StatusCode, response.ReasonPhrase, body);
                return null;
            }

            logger.LogInformation("Groq response OK ({StatusCode})", (int)response.StatusCode);

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception khi gọi Groq API.");
            return null;
        }
    }

    public async Task<(int StatusCode, string Body)> TestRawAsync(CancellationToken ct = default)
    {
        var testBody = JsonSerializer.Serialize(new
        {
            model      = _settings.Model,
            messages   = new[] { new { role = "user", content = "hello" } },
            temperature = 0.7,
            max_tokens  = 1024
        });

        try
        {
            var http = httpFactory.CreateClient("groq");
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await http.PostAsync(_settings.Endpoint,
                new StringContent(testBody, Encoding.UTF8, "application/json"), ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return ((int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            return (0, $"Exception: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
