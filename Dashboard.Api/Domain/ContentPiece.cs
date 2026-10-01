namespace Dashboard.Api.Domain;

public sealed class ContentPiece
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? SeoOpportunityId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string ContentType { get; set; }
    public required string PrimaryKeyword { get; set; }
    public required string SearchIntent { get; set; }
    public required string Hypothesis { get; set; }
    public required string BaselineSummary { get; set; }
    public required string Objective { get; set; }
    public required string Owner { get; set; }
    public required string Brief { get; set; }
    public required string DraftMarkdown { get; set; }
    public required string MetaTitle { get; set; }
    public required string MetaDescription { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset? ScheduledFor { get; set; }
    public DateTimeOffset? MeasuredAt { get; set; }
    public int? ResultImpressions { get; set; }
    public int? ResultClicks { get; set; }
    public required string ResultNotes { get; set; }
    public required string SimulatedWordPressUrl { get; set; }
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public SeoOpportunity? SeoOpportunity { get; set; }
    public ICollection<EditorialHistory> History { get; set; } = [];
}
