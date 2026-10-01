namespace Dashboard.Api.Domain;

public sealed class ExecutionAudit
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ExecutionRecordId { get; set; }
    public required string EventType { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public required string Note { get; set; }
    public required string Actor { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Project Project { get; set; } = null!;
    public ExecutionRecord ExecutionRecord { get; set; } = null!;
}
