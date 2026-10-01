namespace Dashboard.Api.Domain;

public sealed class Project
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Domain { get; set; }
    public required string Type { get; set; }
    public required string Status { get; set; }
    public required string TimeZone { get; set; }
    public required string Environment { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<Campaign> Campaigns { get; set; } = [];
    public ICollection<SocialPost> SocialPosts { get; set; } = [];
    public ICollection<MarketingEvent> MarketingEvents { get; set; } = [];
}
