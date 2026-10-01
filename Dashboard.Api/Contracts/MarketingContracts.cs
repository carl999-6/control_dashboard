using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record CampaignRequest(
    string Name,
    string Objective,
    string Status,
    string UtmCampaign,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record CampaignResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    string Objective,
    string Status,
    string UtmCampaign,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsDemoData,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static CampaignResponse FromEntity(Campaign item) => new(
        item.Id, item.ProjectId, item.Name, item.Objective, item.Status, item.UtmCampaign,
        item.StartDate, item.EndDate, item.IsDemoData, item.CreatedAt, item.UpdatedAt);
}

public sealed record SocialPostRequest(
    Guid? CampaignId,
    string Platform,
    string Topic,
    string Format,
    string Status,
    string ExternalUrl,
    DateTimeOffset? PublishedAt,
    int Impressions,
    int Engagements,
    int Clicks);

public sealed record SocialPostResponse(
    Guid Id,
    Guid ProjectId,
    Guid? CampaignId,
    string Platform,
    string Topic,
    string Format,
    string Status,
    string ExternalUrl,
    string DataSource,
    DateTimeOffset? PublishedAt,
    int Impressions,
    int Engagements,
    int Clicks,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SocialPostResponse FromEntity(SocialPost item) => new(
        item.Id, item.ProjectId, item.CampaignId, item.Platform, item.Topic, item.Format,
        item.Status, item.ExternalUrl, item.DataSource, item.PublishedAt, item.Impressions,
        item.Engagements, item.Clicks, item.CreatedAt, item.UpdatedAt);
}

public sealed record MarketingEventImportItem(
    string Stage,
    string Source,
    string? Medium,
    string? LandingPath,
    int Count,
    DateTimeOffset? OccurredAt,
    Guid? CampaignId,
    Guid? SocialPostId,
    string? DataSource);

public sealed record MarketingImportRequest(IReadOnlyList<MarketingEventImportItem> Events);

public sealed record FunnelStageResponse(string Stage, int Count);
public sealed record SourceMetricResponse(string Source, int Visits, int Contacts);
public sealed record MarketingSummaryResponse(
    IReadOnlyList<FunnelStageResponse> Funnel,
    IReadOnlyList<SourceMetricResponse> Sources,
    int Campaigns,
    int Posts,
    int Impressions,
    int Engagements,
    int Clicks,
    bool ContainsDemoData);
