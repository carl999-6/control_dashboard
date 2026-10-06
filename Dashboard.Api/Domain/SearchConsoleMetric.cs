namespace Dashboard.Api.Domain;

public sealed class SearchConsoleMetric
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid IntegrationConnectionId { get; set; }
    public DateOnly Date { get; set; }
    public required string Query { get; set; }
    public required string Page { get; set; }
    public double Clicks { get; set; }
    public double Impressions { get; set; }
    public double Ctr { get; set; }
    public double Position { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
    public Project Project { get; set; } = null!;
    public IntegrationConnection Connection { get; set; } = null!;
}
