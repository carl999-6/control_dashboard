namespace Dashboard.Api.Services;

public sealed record XRecentPost(
    string Id,
    string Url,
    string AuthorUsername,
    string Text,
    string Language,
    int LikeCount,
    int ReplyCount,
    int RepostCount,
    int QuoteCount,
    int ImpressionCount,
    DateTimeOffset CreatedAt);

public interface IXApiClient
{
    Task<IReadOnlyList<XRecentPost>> SearchRecentAsync(string bearerToken, string query, int maximumResults, CancellationToken cancellationToken);
}

public sealed class XApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
