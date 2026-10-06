using Dashboard.Api.Contracts;
using Dashboard.Api.Services;

namespace Dashboard.Api.Tests;

public sealed class FakePostHogApiClient : IPostHogApiClient
{
    public int Calls { get; private set; }
    public List<PostHogQueryRow> Rows { get; } = [];
    public List<PostHogEventDetailResponse> RecentEvents { get; } = [];
    public PostHogApiException? Failure { get; set; }

    public Task<IReadOnlyList<PostHogQueryRow>> QueryAsync(string region, int projectId, string personalApiKey,
        DateOnly startDate, DateOnly endDateExclusive, int rowLimit, CancellationToken cancellationToken)
    {
        Calls++;
        if (Failure is not null) throw Failure;
        return Task.FromResult<IReadOnlyList<PostHogQueryRow>>(Rows);
    }

    public Task<IReadOnlyList<PostHogEventDetailResponse>> QueryRecentEventsAsync(string region, int projectId,
        string personalApiKey, DateOnly startDate, DateOnly endDateExclusive, string? source, string? campaign,
        string? content, int limit, CancellationToken cancellationToken)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult<IReadOnlyList<PostHogEventDetailResponse>>(RecentEvents.Take(limit).ToList());
    }
}
