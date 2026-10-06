namespace Dashboard.Api.Contracts;

public sealed record GeminiSettingsRequest(
    bool IsEnabled,
    string Model,
    int MinimumImpressions,
    double MinimumPosition,
    double MaximumPosition,
    double MaximumCtrPercent,
    int MaximumSeoDraftsPerDay,
    int MaximumXProposalsPerDay,
    int MaximumTotalGeminiRunsPerDay,
    int MinimumDraftWords,
    int MaximumOutputTokens,
    string Owner);

public sealed record GeminiSettingsResponse(
    bool ApiKeyConfigured,
    bool IsEnabled,
    string Model,
    int MinimumImpressions,
    double MinimumPosition,
    double MaximumPosition,
    double MaximumCtrPercent,
    int MaximumSeoDraftsPerDay,
    int MaximumXProposalsPerDay,
    int MaximumTotalGeminiRunsPerDay,
    int MinimumDraftWords,
    int MaximumOutputTokens,
    string Owner,
    int DraftsGeneratedToday,
    int XProposalsGeneratedToday,
    int TotalGeminiRunsToday,
    DateTimeOffset? UpdatedAt);

public sealed record GeminiSeoResult(
    bool Succeeded,
    string ErrorCode,
    string ErrorMessage,
    Guid? OpportunityId,
    Guid? ContentPieceId,
    string ContentTitle,
    string Model,
    int InputTokens,
    int OutputTokens);
