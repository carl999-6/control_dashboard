namespace Dashboard.Api.Domain;

public sealed class SeoOpportunity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Query { get; set; }
    public required string TargetPage { get; set; }
    public required string Evidence { get; set; }
    public required string Hypothesis { get; set; }
    public required string Status { get; set; }
    public required string DataSource { get; set; }
    public int BaselineImpressions { get; set; }
    public int BaselineClicks { get; set; }
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<ContentPiece> ContentPieces { get; set; } = [];
}
