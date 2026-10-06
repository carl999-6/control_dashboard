namespace Dashboard.Api.Services;

public sealed record WordPressDraftPayload(string Title, string Slug, string ContentHtml, string Excerpt);
public sealed record WordPressPostResult(int Id, string Status, string Link);

public interface IWordPressApiClient
{
    Task ValidateAsync(string baseUrl, string username, string applicationPassword, CancellationToken cancellationToken);
    Task<WordPressPostResult> CreateDraftAsync(string baseUrl, string username, string applicationPassword,
        WordPressDraftPayload payload, CancellationToken cancellationToken);
}

public sealed class WordPressApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
