namespace Dashboard.Api.Domain;

public sealed class ExecutionRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ApiRatePlanId { get; set; }
    public Guid? ParentExecutionId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public required string Flow { get; set; }
    public required string Status { get; set; }
    public bool ApprovalRequired { get; set; }
    public required string ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public int InputUnits { get; set; }
    public int OutputUnits { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostGtq { get; set; }
    public int AttemptNumber { get; set; }
    public required string ErrorCode { get; set; }
    public required string ErrorMessage { get; set; }
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ApiRatePlan ApiRatePlan { get; set; } = null!;
    public ExecutionRecord? ParentExecution { get; set; }
    public ICollection<ExecutionRecord> Retries { get; set; } = [];
    public ICollection<ExecutionAudit> Audit { get; set; } = [];
}
