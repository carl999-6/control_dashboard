using System.Net.Http.Json;
using System.Text.Json;

namespace Dashboard.Api.Services;

public sealed class GeminiApiClient : IGeminiApiClient, IDisposable
{
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    }) { Timeout = TimeSpan.FromSeconds(90) };

    public async Task<GeminiGeneration> GenerateAsync(
        string apiKey,
        string model,
        string prompt,
        int maximumOutputTokens,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.35,
                maxOutputTokens = maximumOutputTokens,
            },
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw CreateException((int)response.StatusCode, body);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var candidate = root.GetProperty("candidates")[0];
            var finishReason = candidate.TryGetProperty("finishReason", out var finish) ? finish.GetString() ?? string.Empty : string.Empty;
            var text = candidate.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
            var usage = root.TryGetProperty("usageMetadata", out var metadata) ? metadata : default;
            var input = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("promptTokenCount", out var promptTokens) ? promptTokens.GetInt32() : 0;
            var output = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("candidatesTokenCount", out var outputTokens) ? outputTokens.GetInt32() : 0;
            if (string.IsNullOrWhiteSpace(text)) throw new GeminiApiException("GEMINI_EMPTY_RESPONSE", "Gemini no devolvió contenido utilizable.");
            return new GeminiGeneration(text, input, output, finishReason);
        }
        catch (GeminiApiException) { throw; }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new GeminiApiException("GEMINI_INVALID_RESPONSE", "Gemini devolvió una respuesta que no se pudo interpretar.");
        }
    }

    private static GeminiApiException CreateException(int statusCode, string body)
    {
        var message = $"Gemini respondió con estado HTTP {statusCode}.";
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                message = detail.GetString()!;
        }
        catch (JsonException) { }

        var code = statusCode == 429 ? "GEMINI_FREE_QUOTA_EXHAUSTED"
            : statusCode is 401 or 403 ? "GEMINI_AUTH_FAILED"
            : statusCode == 404 ? "GEMINI_MODEL_NOT_AVAILABLE"
            : statusCode >= 500 ? "GEMINI_UNAVAILABLE"
            : "GEMINI_API_ERROR";
        return new GeminiApiException(code, message);
    }

    public void Dispose() => _httpClient.Dispose();
}
