namespace Dashboard.Api.Domain;

public sealed class PostHogMetric
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PostHogConnectionId { get; set; }
    public DateOnly Date { get; set; }
    public required string Event { get; set; }
    public required string Source { get; set; }
    public required string Medium { get; set; }
    public required string Campaign { get; set; }
    public required string Content { get; set; }
    public required string Path { get; set; }
    public int EventCount { get; set; }
    public int Sessions { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
    public Project Project { get; set; } = null!;
    public PostHogConnection Connection { get; set; } = null!;
}
