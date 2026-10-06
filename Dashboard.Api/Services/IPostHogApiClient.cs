using Dashboard.Api.Contracts;

namespace Dashboard.Api.Services;

public interface IPostHogApiClient
{
    Task<IReadOnlyList<PostHogQueryRow>> QueryAsync(
        string region, int projectId, string personalApiKey, DateOnly startDate,
        DateOnly endDateExclusive, int rowLimit, CancellationToken cancellationToken);

    Task<IReadOnlyList<PostHogEventDetailResponse>> QueryRecentEventsAsync(
        string region, int projectId, string personalApiKey, DateOnly startDate,
        DateOnly endDateExclusive, string? source, string? campaign, string? content,
        int limit, CancellationToken cancellationToken);
}

public sealed class PostHogApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
