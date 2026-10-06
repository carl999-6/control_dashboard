namespace Dashboard.Api.Contracts;

public sealed record SearchConsoleStatusResponse(
    bool ClientConfigured,
    bool Connected,
    string Status,
    string Property,
    string PermissionLevel,
    int LookbackDays,
    int RowLimit,
    DateTimeOffset? LastSyncAt,
    string LastError);

public sealed record SearchConsoleAuthorizationResponse(string AuthorizationUrl);

public sealed record SearchConsoleSettingsRequest(int LookbackDays, int RowLimit);

public sealed record SearchConsoleDisconnectRequest(string Confirmation);

public sealed record SearchConsoleSyncResult(
    bool Succeeded,
    string ErrorCode,
    string ErrorMessage,
    int ImportedRows,
    double Clicks,
    double Impressions,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record SearchConsoleQueryMetric(
    string Query,
    string Page,
    double Clicks,
    double Impressions,
    double Ctr,
    double Position);

public sealed record SearchConsoleSummaryResponse(
    int Rows,
    double Clicks,
    double Impressions,
    double Ctr,
    double AveragePosition,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<SearchConsoleQueryMetric> TopQueries);
