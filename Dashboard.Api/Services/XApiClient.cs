using System.Net.Http.Headers;
using System.Text.Json;

namespace Dashboard.Api.Services;

public sealed class XApiClient : IXApiClient, IDisposable
{
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    }) { Timeout = TimeSpan.FromSeconds(45) };

    public async Task<IReadOnlyList<XRecentPost>> SearchRecentAsync(string bearerToken, string query, int maximumResults, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["query"] = query,
            ["max_results"] = Math.Clamp(maximumResults, 10, 100).ToString(),
            ["tweet.fields"] = "created_at,lang,public_metrics,author_id",
            ["expansions"] = "author_id",
            ["user.fields"] = "username",
        };
        var url = QueryString.Create(parameters).ToUriComponent();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.x.com/2/tweets/search/recent{url}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var response = await SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw CreateException((int)response.StatusCode, body);
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var users = new Dictionary<string, string>();
            if (root.TryGetProperty("includes", out var includes) && includes.TryGetProperty("users", out var userItems))
                foreach (var user in userItems.EnumerateArray()) users[user.GetProperty("id").GetString() ?? string.Empty] = user.GetProperty("username").GetString() ?? string.Empty;
            if (!root.TryGetProperty("data", out var data)) return [];
            var result = new List<XRecentPost>();
            foreach (var item in data.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                var authorId = item.TryGetProperty("author_id", out var author) ? author.GetString() ?? string.Empty : string.Empty;
                users.TryGetValue(authorId, out var username);
                var metrics = item.TryGetProperty("public_metrics", out var publicMetrics) ? publicMetrics : default;
                result.Add(new XRecentPost(
                    id, $"https://x.com/{username ?? "i"}/status/{id}", username ?? string.Empty,
                    item.GetProperty("text").GetString() ?? string.Empty,
                    item.TryGetProperty("lang", out var language) ? language.GetString() ?? string.Empty : string.Empty,
                    Metric(metrics, "like_count"), Metric(metrics, "reply_count"), Metric(metrics, "retweet_count", "repost_count"),
                    Metric(metrics, "quote_count"), Metric(metrics, "impression_count"),
                    item.TryGetProperty("created_at", out var created) && DateTimeOffset.TryParse(created.GetString(), out var date) ? date : DateTimeOffset.UtcNow));
            }
            return result;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new XApiException("X_INVALID_RESPONSE", "X devolvió una respuesta que no se pudo interpretar.");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try { return await _httpClient.SendAsync(request, cancellationToken); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new XApiException("X_TIMEOUT", "X no respondió antes de que venciera el tiempo de espera."); }
        catch (HttpRequestException)
        { throw new XApiException("X_UNAVAILABLE", "No fue posible conectar con X."); }
    }

    private static int Metric(JsonElement metrics, string primary, string? fallback = null)
    {
        if (metrics.ValueKind != JsonValueKind.Object) return 0;
        if (metrics.TryGetProperty(primary, out var value) && value.TryGetInt32(out var number)) return number;
        return fallback is not null && metrics.TryGetProperty(fallback, out value) && value.TryGetInt32(out number) ? number : 0;
    }

    private static XApiException CreateException(int status, string body)
    {
        var message = $"X respondió con estado HTTP {status}.";
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString())) message = detail.GetString()!;
            else if (document.RootElement.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString())) message = title.GetString()!;
        }
        catch (JsonException) { }
        var code = status == 402 ? "X_CREDITS_EXHAUSTED" : status == 429 ? "X_RATE_LIMITED"
            : status is 401 or 403 ? "X_AUTH_FAILED" : status >= 500 ? "X_UNAVAILABLE" : "X_API_ERROR";
        return new XApiException(code, message);
    }

    public void Dispose() => _httpClient.Dispose();
}
