namespace Dashboard.Api.Domain;

public sealed class ApiRatePlan
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public decimal InputUsdPerMillion { get; set; }
    public decimal OutputUsdPerMillion { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public bool IsActive { get; set; }
    public bool IsDemoData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<ExecutionRecord> Executions { get; set; } = [];
}
