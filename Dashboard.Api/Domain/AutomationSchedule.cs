namespace Dashboard.Api.Domain;

public sealed class AutomationSchedule
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Workflow { get; set; }
    public required string DisplayName { get; set; }
    public bool IsEnabled { get; set; }
    public required string Frequency { get; set; }
    public int? IntervalMinutes { get; set; }
    public required string LocalTime { get; set; }
    public int? DayOfWeek { get; set; }
    public int MaxRunsPerDay { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<AutomationRun> Runs { get; set; } = [];
}
