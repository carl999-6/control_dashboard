namespace Dashboard.Api.Domain;

public sealed class EditorialHistory
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ContentPieceId { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public required string Note { get; set; }
    public required string Actor { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ContentPiece ContentPiece { get; set; } = null!;
}
