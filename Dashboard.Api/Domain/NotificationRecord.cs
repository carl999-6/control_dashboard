namespace Dashboard.Api.Domain;

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid NotificationPolicyId { get; set; }
    public required string DeduplicationKey { get; set; }
    public required string Category { get; set; }
    public required string Severity { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Flow { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public long? InputUnits { get; set; }
    public long? OutputUnits { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public required string Status { get; set; }
    public int GroupCount { get; set; }
    public int AttemptCount { get; set; }
    public bool SimulateFailure { get; set; }
    public required string ErrorMessage { get; set; }
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public Project Project { get; set; } = null!;
    public NotificationPolicy NotificationPolicy { get; set; } = null!;
}
