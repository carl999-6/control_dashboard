using Dashboard.Api.Services;

namespace Dashboard.Api.Tests;

public sealed class FakeWordPressApiClient : IWordPressApiClient
{
    public int CreateCalls { get; private set; }
    public bool FailNextCreate { get; set; }

    public Task ValidateAsync(string baseUrl, string username, string applicationPassword, CancellationToken cancellationToken)
    {
        if (baseUrl != "https://fyrstudios.com" || username != "dashboard-bot" || applicationPassword != "test-wordpress-password")
            throw new WordPressApiException("WORDPRESS_AUTH_FAILED", "Credenciales de prueba no válidas.");
        return Task.CompletedTask;
    }

    public Task<WordPressPostResult> CreateDraftAsync(string baseUrl, string username, string applicationPassword,
        WordPressDraftPayload payload, CancellationToken cancellationToken)
    {
        CreateCalls++;
        if (FailNextCreate)
        {
            FailNextCreate = false;
            throw new WordPressApiException("WORDPRESS_UNAVAILABLE", "Fallo recuperable de prueba.");
        }
        if (string.IsNullOrWhiteSpace(payload.ContentHtml)) throw new WordPressApiException("WORDPRESS_EMPTY_DRAFT", "Falta contenido.");
        return Task.FromResult(new WordPressPostResult(781, "draft", "https://fyrstudios.com/?p=781"));
    }
}
