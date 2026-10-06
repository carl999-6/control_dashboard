using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Dashboard.Api.Services;

public sealed class WordPressApiClient : IWordPressApiClient, IDisposable
{
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    }) { Timeout = TimeSpan.FromSeconds(45) };

    public async Task ValidateAsync(string baseUrl, string username, string applicationPassword, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{NormalizeBaseUrl(baseUrl)}/wp-json/wp/v2/users/me?context=edit", username, applicationPassword);
        using var response = await SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await CreateException(response, cancellationToken);
    }

    public async Task<WordPressPostResult> CreateDraftAsync(string baseUrl, string username, string applicationPassword,
        WordPressDraftPayload payload, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{NormalizeBaseUrl(baseUrl)}/wp-json/wp/v2/posts", username, applicationPassword);
        request.Content = JsonContent.Create(new
        {
            title = payload.Title,
            slug = payload.Slug,
            content = payload.ContentHtml,
            excerpt = payload.Excerpt,
            status = "draft",
            comment_status = "closed",
            ping_status = "closed",
        });
        using var response = await SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await CreateException(response, cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            var result = new WordPressPostResult(
                root.GetProperty("id").GetInt32(),
                root.GetProperty("status").GetString() ?? string.Empty,
                root.TryGetProperty("link", out var link) ? link.GetString() ?? string.Empty : string.Empty);
            if (result.Status != "draft")
                throw new WordPressApiException("WORDPRESS_UNSAFE_STATUS", "WordPress no confirmó el estado draft; el flujo se detuvo por seguridad.");
            return result;
        }
        catch (WordPressApiException) { throw; }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new WordPressApiException("WORDPRESS_INVALID_RESPONSE", "WordPress devolvió una respuesta que no se pudo interpretar.");
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string username, string password)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string NormalizeBaseUrl(string value) => value.Trim().TrimEnd('/');

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try { return await _httpClient.SendAsync(request, cancellationToken); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WordPressApiException("WORDPRESS_TIMEOUT", "WordPress no respondió antes de que venciera el tiempo de espera.");
        }
        catch (HttpRequestException)
        {
            throw new WordPressApiException("WORDPRESS_UNAVAILABLE", "No fue posible conectar con WordPress. El borrador local se conservó.");
        }
    }

    private static async Task<WordPressApiException> CreateException(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var detail = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("message", out var message)) detail = message.GetString() ?? string.Empty;
        }
        catch (JsonException) { }
        var code = status is 401 or 403 ? "WORDPRESS_AUTH_FAILED"
            : status == 404 ? "WORDPRESS_REST_API_NOT_FOUND"
            : status >= 500 ? "WORDPRESS_UNAVAILABLE"
            : "WORDPRESS_API_ERROR";
        return new WordPressApiException(code, string.IsNullOrWhiteSpace(detail) ? $"WordPress respondió con estado HTTP {status}." : detail);
    }

    public void Dispose() => _httpClient.Dispose();
}
