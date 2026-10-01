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
    public ICollection<SeoOpportunity> SeoOpportunities { get; set; } = [];
    public ICollection<ContentPiece> ContentPieces { get; set; } = [];
    public ICollection<EditorialHistory> EditorialHistory { get; set; } = [];
    public ICollection<ApiRatePlan> ApiRatePlans { get; set; } = [];
    public ProjectBudget? Budget { get; set; }
    public ICollection<ExecutionRecord> Executions { get; set; } = [];
    public ICollection<ExecutionAudit> ExecutionAudit { get; set; } = [];
}
