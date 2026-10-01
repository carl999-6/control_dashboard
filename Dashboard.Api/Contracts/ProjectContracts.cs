using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record ProjectRequest(string Name, string Domain, string Type, string Status, string TimeZone, string Environment, string? Description);
public sealed record GoalRequest(string Title, string? Description, string? Metric, decimal? TargetValue, string Status, DateTimeOffset? DueDate);
public sealed record PreferenceRequest(string TimeZone, string Currency, int DefaultDateRangeDays, bool CompactNotifications);
public sealed record LoginRequest(string Password);

public sealed record ProjectResponse(
    Guid Id, string Name, string Slug, string Domain, string Type, string Status, string TimeZone,
    string Environment, string Description, bool IsDemoData, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyCollection<GoalResponse> Goals)
{
    public static ProjectResponse FromEntity(Project project) => new(
        project.Id, project.Name, project.Slug, project.Domain, project.Type, project.Status, project.TimeZone,
        project.Environment, project.Description, project.IsDemoData, project.CreatedAt, project.UpdatedAt,
        project.Goals.Select(GoalResponse.FromEntity).ToArray());
}

public sealed record GoalResponse(
    Guid Id, Guid ProjectId, string Title, string Description, string Metric, decimal? TargetValue,
    string Status, DateTimeOffset? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static GoalResponse FromEntity(Goal goal) => new(
        goal.Id, goal.ProjectId, goal.Title, goal.Description, goal.Metric, goal.TargetValue,
        goal.Status, goal.DueDate, goal.CreatedAt, goal.UpdatedAt);
}
