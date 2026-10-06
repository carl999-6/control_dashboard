namespace Dashboard.Api.Domain;

public sealed class XAssistantSettings
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public bool IsEnabled { get; set; }
    public bool ApiReadEnabled { get; set; }
    public required string SearchQuery { get; set; }
    public required string Language { get; set; }
    public int MaximumPostsPerSync { get; set; }
    public decimal ReadCostUsdPerPost { get; set; }
    public required string ToneInstructions { get; set; }
    public required string LandingPath { get; set; }
    public required string UtmCampaign { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public required string LastError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
}
