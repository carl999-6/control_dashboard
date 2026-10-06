using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record PostHogSettingsRequest(
    string Region, int ExternalProjectId, string PublicToken, string ApiKeyEnvironmentVariable,
    int LookbackDays, int RowLimit, bool IsEnabled, bool ConfirmReplaceData = false);

public sealed record PostHogStatusResponse(
    bool IsConfigured, bool KeyAvailable, bool IsEnabled, string Region, int ExternalProjectId,
    string PublicToken, string ApiKeyEnvironmentVariable, int LookbackDays, int RowLimit,
    DateTimeOffset? LastSyncAt, string LastError)
{
    public static PostHogStatusResponse FromEntity(PostHogConnection? item, bool keyAvailable) => item is null
        ? new(false, false, false, "us", 0, string.Empty, "POSTHOG_PERSONAL_API_KEY", 7, 5000, null, string.Empty)
        : new(true, keyAvailable, item.IsEnabled, item.Region, item.ExternalProjectId,
            item.PublicToken, item.ApiKeyEnvironmentVariable, item.LookbackDays, item.RowLimit,
            item.LastSyncAt, item.LastError);
}

public sealed record PostHogMetricResponse(
    DateOnly Date, string Event, string Source, string Medium, string Campaign,
    string Content, string Path, int EventCount, int Sessions, Guid? XReplyProposalId, Guid? CampaignId);

public sealed record PostHogSummaryResponse(
    int Pageviews, int Sessions, int QuoteRequests, int WhatsAppClicks,
    IReadOnlyList<PostHogMetricResponse> Metrics,
    IReadOnlyList<PostHogAttributionResponse> Attribution,
    DateTimeOffset? LastSyncAt);

public sealed record PostHogAttributionResponse(
    string Source, string Campaign, string Content, int Visits, int Pageviews, int QuoteRequests,
    int WhatsAppClicks, Guid? XReplyProposalId, Guid? CampaignId);

public sealed record PostHogSyncResult(
    bool Succeeded, string ErrorCode, string ErrorMessage, int ImportedRows,
    int Pageviews, int QuoteRequests, int WhatsAppClicks);

public sealed record PostHogQueryRow(
    DateOnly Date, string Event, string Source, string Medium, string Campaign,
    string Content, string Path, int EventCount, int Sessions);

public sealed record PostHogEventDetailResponse(
    DateTimeOffset OccurredAt, string Event, string Path, string Source, string Medium,
    string Campaign, string Content, string Attribution, string ReferrerDomain,
    string Browser, string City, string Country, string DeviceType, string Os, string OsVersion);
