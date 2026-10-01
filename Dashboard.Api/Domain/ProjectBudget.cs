namespace Dashboard.Api.Domain;

public sealed class ProjectBudget
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public decimal DailyLimitUsd { get; set; }
    public decimal MonthlyLimitUsd { get; set; }
    public int WarningPercent { get; set; }
    public decimal ExchangeRateGtqPerUsd { get; set; }
    public bool IsPaused { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
}
