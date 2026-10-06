using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record AutomationScheduleRequest(
    bool IsEnabled,
    string Frequency,
    int? IntervalMinutes,
    string LocalTime,
    int? DayOfWeek,
    int MaxRunsPerDay);

public sealed record AutomationScheduleResponse(
    Guid Id,
    Guid ProjectId,
    string Workflow,
    string DisplayName,
    bool IsEnabled,
    string Frequency,
    int? IntervalMinutes,
    string LocalTime,
    int? DayOfWeek,
    int MaxRunsPerDay,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastRunAt,
    DateTimeOffset UpdatedAt)
{
    public static AutomationScheduleResponse FromEntity(AutomationSchedule item) => new(
        item.Id, item.ProjectId, item.Workflow, item.DisplayName, item.IsEnabled,
        item.Frequency, item.IntervalMinutes, item.LocalTime, item.DayOfWeek,
        item.MaxRunsPerDay, item.NextRunAt, item.LastRunAt, item.UpdatedAt);
}

public sealed record AutomationRunResponse(
    Guid Id,
    Guid ProjectId,
    Guid AutomationScheduleId,
    string Trigger,
    string Status,
    string ErrorCode,
    string ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt)
{
    public static AutomationRunResponse FromEntity(AutomationRun item) => new(
        item.Id, item.ProjectId, item.AutomationScheduleId, item.Trigger, item.Status,
        item.ErrorCode, item.ErrorMessage, item.CreatedAt, item.StartedAt, item.CompletedAt);
}
