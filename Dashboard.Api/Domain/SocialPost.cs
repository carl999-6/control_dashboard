namespace Dashboard.Api.Domain;

public sealed class SocialPost
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }
    public required string Platform { get; set; }
    public required string Topic { get; set; }
    public required string Format { get; set; }
    public required string Status { get; set; }
    public required string ExternalUrl { get; set; }
    public required string DataSource { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int Impressions { get; set; }
    public int Engagements { get; set; }
    public int Clicks { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public Campaign? Campaign { get; set; }
    public ICollection<MarketingEvent> Events { get; set; } = [];
}
