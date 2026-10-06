namespace Dashboard.Api.Domain;

public sealed class PostHogConnection
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Region { get; set; }
    public int ExternalProjectId { get; set; }
    public required string PublicToken { get; set; }
    public required string ApiKeyEnvironmentVariable { get; set; }
    public int LookbackDays { get; set; }
    public int RowLimit { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public required string LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<PostHogMetric> Metrics { get; set; } = [];
}
