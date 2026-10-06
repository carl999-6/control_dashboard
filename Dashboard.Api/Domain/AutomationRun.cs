namespace Dashboard.Api.Domain;

public sealed class AutomationRun
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid AutomationScheduleId { get; set; }
    public required string Trigger { get; set; }
    public required string Status { get; set; }
    public required string ErrorCode { get; set; }
    public required string ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Project Project { get; set; } = null!;
    public AutomationSchedule Schedule { get; set; } = null!;
}
