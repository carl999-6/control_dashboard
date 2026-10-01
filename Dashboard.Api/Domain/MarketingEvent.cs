namespace Dashboard.Api.Domain;

public sealed class MarketingEvent
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? SocialPostId { get; set; }
    public required string Stage { get; set; }
    public required string Source { get; set; }
    public required string Medium { get; set; }
    public required string LandingPath { get; set; }
    public required string DataSource { get; set; }
    public int Count { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public Campaign? Campaign { get; set; }
    public SocialPost? SocialPost { get; set; }
}
