namespace Dashboard.Api.Domain;

public sealed class DashboardPreference
{
    public int Id { get; set; }
    public required string TimeZone { get; set; }
    public required string Currency { get; set; }
    public int DefaultDateRangeDays { get; set; }
    public bool CompactNotifications { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
