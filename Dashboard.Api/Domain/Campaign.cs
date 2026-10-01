namespace Dashboard.Api.Domain;

public sealed class Campaign
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Name { get; set; }
    public required string Objective { get; set; }
    public required string Status { get; set; }
    public required string UtmCampaign { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<SocialPost> Posts { get; set; } = [];
    public ICollection<MarketingEvent> Events { get; set; } = [];
}
