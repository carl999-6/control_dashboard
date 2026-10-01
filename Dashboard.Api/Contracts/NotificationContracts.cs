using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record NotificationPolicyRequest(
    bool IsEnabled,
    string MinimumSeverity,
    int GroupWindowMinutes,
    int QuietHoursStart,
    int QuietHoursEnd);

public sealed record NotificationPolicyResponse(
    Guid Id,
    Guid ProjectId,
    bool IsEnabled,
    string DeliveryMode,
    string MinimumSeverity,
    int GroupWindowMinutes,
    int QuietHoursStart,
    int QuietHoursEnd,
    DateTimeOffset UpdatedAt)
{
    public static NotificationPolicyResponse FromEntity(NotificationPolicy item) => new(
        item.Id, item.ProjectId, item.IsEnabled, item.DeliveryMode, item.MinimumSeverity,
        item.GroupWindowMinutes, item.QuietHoursStart, item.QuietHoursEnd, item.UpdatedAt);
}

public sealed record DispatchNotificationRequest(
    string DeduplicationKey,
    string Category,
    string Severity,
    string Title,
    string Message,
    bool SimulateFailure);

public sealed record NotificationResponse(
    Guid Id,
    Guid ProjectId,
    string DeduplicationKey,
    string Category,
    string Severity,
    string Title,
    string Message,
    string Status,
    int GroupCount,
    int AttemptCount,
    bool SimulateFailure,
    string ErrorMessage,
    bool IsDemoData,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? DeliveredAt)
{
    public static NotificationResponse FromEntity(NotificationRecord item) => new(
        item.Id, item.ProjectId, item.DeduplicationKey, item.Category, item.Severity,
        item.Title, item.Message, item.Status, item.GroupCount, item.AttemptCount,
        item.SimulateFailure, item.ErrorMessage, item.IsDemoData, item.CreatedAt,
        item.UpdatedAt, item.LastAttemptAt, item.DeliveredAt);
}

public sealed record NotificationSummaryResponse(
    int Delivered,
    int Pending,
    int Failed,
    int Grouped,
    int Suppressed,
    int TotalOccurrences);
