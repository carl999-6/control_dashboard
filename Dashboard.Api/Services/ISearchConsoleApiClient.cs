using System.Text.Json.Serialization;

namespace Dashboard.Api.Services;

public sealed record SearchConsoleToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("token_type")] string? TokenType);

public sealed record SearchConsoleSite(string SiteUrl, string PermissionLevel);

public sealed record SearchConsoleDataRow(
    IReadOnlyList<string> Keys,
    double Clicks,
    double Impressions,
    double Ctr,
    double Position);

public interface ISearchConsoleApiClient
{
    Task<SearchConsoleToken> ExchangeCodeAsync(string code, string redirectUri, string clientId, string clientSecret, CancellationToken cancellationToken);
    Task<SearchConsoleToken> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchConsoleSite>> ListSitesAsync(string accessToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchConsoleDataRow>> QueryAsync(string accessToken, string siteUrl, DateOnly startDate, DateOnly endDate, int rowLimit, CancellationToken cancellationToken);
}

public sealed class SearchConsoleApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
