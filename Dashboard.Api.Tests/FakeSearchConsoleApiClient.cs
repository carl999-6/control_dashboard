using Dashboard.Api.Services;

namespace Dashboard.Api.Tests;

public sealed class FakeSearchConsoleApiClient : ISearchConsoleApiClient
{
    public Task<SearchConsoleToken> ExchangeCodeAsync(string code, string redirectUri, string clientId, string clientSecret, CancellationToken cancellationToken) =>
        Task.FromResult(new SearchConsoleToken("access-test", "refresh-test", 3600, SearchConsoleService.ReadOnlyScope, "Bearer"));

    public Task<SearchConsoleToken> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret, CancellationToken cancellationToken) =>
        Task.FromResult(new SearchConsoleToken("access-refreshed", null, 3600, SearchConsoleService.ReadOnlyScope, "Bearer"));

    public Task<IReadOnlyList<SearchConsoleSite>> ListSitesAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SearchConsoleSite>>([new SearchConsoleSite("sc-domain:fyrstudios.com", "siteOwner")]);

    public Task<IReadOnlyList<SearchConsoleDataRow>> QueryAsync(string accessToken, string siteUrl, DateOnly startDate, DateOnly endDate, int rowLimit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SearchConsoleDataRow>>([
            new SearchConsoleDataRow([endDate.AddDays(-1).ToString("yyyy-MM-dd"), "diseño web guatemala", "https://fyrstudios.com/servicios/"], 8, 320, 0.025, 8.4),
            new SearchConsoleDataRow([endDate.ToString("yyyy-MM-dd"), "agencia creativa", "https://fyrstudios.com/"], 5, 180, 0.027777, 11.2),
        ]);
}
