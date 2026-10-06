using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dashboard.Api.Services;

public sealed class GoogleSearchConsoleApiClient : ISearchConsoleApiClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    }) { Timeout = TimeSpan.FromSeconds(30) };

    public Task<SearchConsoleToken> ExchangeCodeAsync(string code, string redirectUri, string clientId, string clientSecret, CancellationToken cancellationToken) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        }, cancellationToken);

    public Task<SearchConsoleToken> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret, CancellationToken cancellationToken) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token",
        }, cancellationToken);

    public async Task<IReadOnlyList<SearchConsoleSite>> ListSitesAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = Authorized(HttpMethod.Get, "https://www.googleapis.com/webmasters/v3/sites", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<SitesResponse>(JsonOptions, cancellationToken);
        return payload?.SiteEntry ?? [];
    }

    public async Task<IReadOnlyList<SearchConsoleDataRow>> QueryAsync(string accessToken, string siteUrl, DateOnly startDate, DateOnly endDate, int rowLimit, CancellationToken cancellationToken)
    {
        var encodedSite = Uri.EscapeDataString(siteUrl);
        using var request = Authorized(HttpMethod.Post, $"https://www.googleapis.com/webmasters/v3/sites/{encodedSite}/searchAnalytics/query", accessToken);
        request.Content = JsonContent.Create(new
        {
            startDate = startDate.ToString("yyyy-MM-dd"),
            endDate = endDate.ToString("yyyy-MM-dd"),
            dimensions = new[] { "date", "query", "page" },
            type = "web",
            dataState = "final",
            rowLimit,
            startRow = 0,
        });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<QueryResponse>(JsonOptions, cancellationToken);
        return payload?.Rows ?? [];
    }

    private async Task<SearchConsoleToken> RequestTokenAsync(IReadOnlyDictionary<string, string> fields, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(fields), cancellationToken);
        await EnsureSuccess(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<SearchConsoleToken>(JsonOptions, cancellationToken)
            ?? throw new SearchConsoleApiException("INVALID_TOKEN_RESPONSE", "Google devolvió una respuesta de autenticación vacía.");
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var status = (int)response.StatusCode;
        var code = status == 429 ? "SEARCH_CONSOLE_QUOTA_EXHAUSTED" : status is 401 or 403 ? "SEARCH_CONSOLE_AUTH_FAILED" : "SEARCH_CONSOLE_API_ERROR";
        string message;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<GoogleErrorEnvelope>(JsonOptions, cancellationToken);
            message = error?.Error?.Message ?? $"Google respondió con estado HTTP {status}.";
        }
        catch (JsonException)
        {
            message = $"Google respondió con estado HTTP {status}.";
        }
        throw new SearchConsoleApiException(code, message);
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed record SitesResponse([property: JsonPropertyName("siteEntry")] List<SearchConsoleSite>? SiteEntry);
    private sealed record QueryResponse(List<SearchConsoleDataRow>? Rows);
    private sealed record GoogleErrorEnvelope(GoogleError? Error);
    private sealed record GoogleError(string? Message);
}
