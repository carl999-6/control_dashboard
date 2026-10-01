namespace Dashboard.Api.Domain;

public sealed class NotificationPolicy
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public bool IsEnabled { get; set; }
    public required string DeliveryMode { get; set; }
    public required string MinimumSeverity { get; set; }
    public int GroupWindowMinutes { get; set; }
    public int QuietHoursStart { get; set; }
    public int QuietHoursEnd { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
}
