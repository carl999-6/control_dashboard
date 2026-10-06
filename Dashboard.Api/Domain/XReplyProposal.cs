namespace Dashboard.Api.Domain;

public sealed class XReplyProposal
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid XSourcePostId { get; set; }
    public required string RecommendedReply { get; set; }
    public required string AlternativeOne { get; set; }
    public required string AlternativeTwo { get; set; }
    public required string SelectedReply { get; set; }
    public required string Rationale { get; set; }
    public required string RiskNotes { get; set; }
    public required string Status { get; set; }
    public required string PublishedReplyUrl { get; set; }
    public required string TrackingUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public XSourcePost SourcePost { get; set; } = null!;
}
