using Dashboard.Api.Services;

namespace Dashboard.Api.Tests;

public sealed class FakeXApiClient : IXApiClient
{
    public int SearchCalls { get; private set; }

    public Task<IReadOnlyList<XRecentPost>> SearchRecentAsync(string bearerToken, string query, int maximumResults, CancellationToken cancellationToken)
    {
        SearchCalls++;
        IReadOnlyList<XRecentPost> posts =
        [
            new("998877665544", "https://x.com/diseno_gt/status/998877665544", "diseno_gt",
                "Antes de diseñar una web conviene definir qué problema debe resolver.", "es", 18, 2, 3, 1, 950, DateTimeOffset.UtcNow.AddHours(-2)),
        ];
        return Task.FromResult(posts);
    }
}
