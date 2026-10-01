using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record RatePlanRequest(
    string Provider,
    string Model,
    decimal InputUsdPerMillion,
    decimal OutputUsdPerMillion,
    DateTimeOffset? EffectiveFrom);

public sealed record RatePlanResponse(
    Guid Id,
    Guid ProjectId,
    string Provider,
    string Model,
    decimal InputUsdPerMillion,
    decimal OutputUsdPerMillion,
    DateTimeOffset EffectiveFrom,
    bool IsActive,
    bool IsDemoData)
{
    public static RatePlanResponse FromEntity(ApiRatePlan item) => new(
        item.Id, item.ProjectId, item.Provider, item.Model, item.InputUsdPerMillion,
        item.OutputUsdPerMillion, item.EffectiveFrom, item.IsActive, item.IsDemoData);
}

public sealed record BudgetRequest(
    decimal DailyLimitUsd,
    decimal MonthlyLimitUsd,
    int WarningPercent,
    decimal ExchangeRateGtqPerUsd,
    bool IsPaused);

public sealed record BudgetResponse(
    Guid Id,
    Guid ProjectId,
    decimal DailyLimitUsd,
    decimal MonthlyLimitUsd,
    int WarningPercent,
    decimal ExchangeRateGtqPerUsd,
    bool IsPaused,
    DateTimeOffset UpdatedAt)
{
    public static BudgetResponse FromEntity(ProjectBudget item) => new(
        item.Id, item.ProjectId, item.DailyLimitUsd, item.MonthlyLimitUsd,
        item.WarningPercent, item.ExchangeRateGtqPerUsd, item.IsPaused, item.UpdatedAt);
}

public sealed record PlanExecutionRequest(
    string IdempotencyKey,
    string Provider,
    string Model,
    string Flow,
    int InputUnits,
    int OutputUnits,
    bool ApprovalRequired);

public sealed record CompleteExecutionRequest(bool Succeeded, string? ErrorCode, string? ErrorMessage);
public sealed record ApprovalRequest(string? Note);

public sealed record ExecutionAuditResponse(
    Guid Id,
    string EventType,
    string FromStatus,
    string ToStatus,
    string Note,
    string Actor,
    DateTimeOffset OccurredAt)
{
    public static ExecutionAuditResponse FromEntity(ExecutionAudit item) => new(
        item.Id, item.EventType, item.FromStatus, item.ToStatus, item.Note, item.Actor, item.OccurredAt);
}

public sealed record ExecutionResponse(
    Guid Id,
    Guid ProjectId,
    Guid ApiRatePlanId,
    Guid? ParentExecutionId,
    string IdempotencyKey,
    string Provider,
    string Model,
    string Flow,
    string Status,
    bool ApprovalRequired,
    string ApprovedBy,
    DateTimeOffset? ApprovedAt,
    int InputUnits,
    int OutputUnits,
    decimal EstimatedCostUsd,
    decimal EstimatedCostGtq,
    int AttemptNumber,
    string ErrorCode,
    string ErrorMessage,
    bool IsDemoData,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ExecutionAuditResponse> Audit)
{
    public static ExecutionResponse FromEntity(ExecutionRecord item) => new(
        item.Id, item.ProjectId, item.ApiRatePlanId, item.ParentExecutionId, item.IdempotencyKey,
        item.Provider, item.Model, item.Flow, item.Status, item.ApprovalRequired, item.ApprovedBy,
        item.ApprovedAt, item.InputUnits, item.OutputUnits, item.EstimatedCostUsd,
        item.EstimatedCostGtq, item.AttemptNumber, item.ErrorCode, item.ErrorMessage,
        item.IsDemoData, item.CreatedAt, item.StartedAt, item.CompletedAt,
        item.Audit.OrderByDescending(entry => entry.OccurredAt).Select(ExecutionAuditResponse.FromEntity).ToList());
}

public sealed record ProviderCostResponse(string Provider, decimal CostUsd, decimal CostGtq, int Executions);
public sealed record ExecutionSummaryResponse(
    decimal TodayCostUsd,
    decimal MonthCostUsd,
    decimal MonthCostGtq,
    decimal MonthBudgetUsd,
    decimal BudgetUsedPercent,
    int AwaitingApproval,
    int Blocked,
    int Failed,
    int Succeeded,
    bool IsPaused,
    IReadOnlyList<ProviderCostResponse> Providers);
