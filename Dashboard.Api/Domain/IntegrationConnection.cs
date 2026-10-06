namespace Dashboard.Api.Domain;

public sealed class IntegrationConnection
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Provider { get; set; }
    public required string Status { get; set; }
    public required string ExternalAccount { get; set; }
    public required string ResourceId { get; set; }
    public required string EncryptedRefreshToken { get; set; }
    public required string GrantedScopes { get; set; }
    public int LookbackDays { get; set; }
    public int RowLimit { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public required string LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<SearchConsoleMetric> SearchConsoleMetrics { get; set; } = [];
}
