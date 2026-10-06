namespace Dashboard.Api.Domain;

public sealed class OAuthStateRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Provider { get; set; }
    public required string StateHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Project Project { get; set; } = null!;
}
