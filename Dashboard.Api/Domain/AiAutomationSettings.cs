namespace Dashboard.Api.Domain;

public sealed class AiAutomationSettings
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public bool IsEnabled { get; set; }
    public required string Model { get; set; }
    public int MinimumImpressions { get; set; }
    public double MinimumPosition { get; set; }
    public double MaximumPosition { get; set; }
    public double MaximumCtr { get; set; }
    public int MaximumSeoDraftsPerDay { get; set; }
    public int MaximumXProposalsPerDay { get; set; }
    public int MaximumTotalGeminiRunsPerDay { get; set; }
    public int MinimumDraftWords { get; set; }
    public int MaximumOutputTokens { get; set; }
    public required string Owner { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
}
