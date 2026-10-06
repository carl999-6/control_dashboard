namespace Dashboard.Api.Domain;

public sealed class XSourcePost
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string ExternalPostId { get; set; }
    public required string Url { get; set; }
    public required string AuthorUsername { get; set; }
    public required string Text { get; set; }
    public required string Language { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int QuoteCount { get; set; }
    public int ImpressionCount { get; set; }
    public required string Status { get; set; }
    public required string DataSource { get; set; }
    public DateTimeOffset PostedAt { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<XReplyProposal> Proposals { get; set; } = [];
}
