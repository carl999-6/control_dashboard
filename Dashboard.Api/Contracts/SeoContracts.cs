using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record SeoOpportunityRequest(
    string Query,
    string TargetPage,
    string Evidence,
    string Hypothesis,
    string Status,
    int BaselineImpressions,
    int BaselineClicks);

public sealed record SeoOpportunityResponse(
    Guid Id,
    Guid ProjectId,
    string Query,
    string TargetPage,
    string Evidence,
    string Hypothesis,
    string Status,
    string DataSource,
    int BaselineImpressions,
    int BaselineClicks,
    bool IsDemoData,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SeoOpportunityResponse FromEntity(SeoOpportunity item) => new(
        item.Id, item.ProjectId, item.Query, item.TargetPage, item.Evidence, item.Hypothesis,
        item.Status, item.DataSource, item.BaselineImpressions, item.BaselineClicks,
        item.IsDemoData, item.CreatedAt, item.UpdatedAt);
}

public sealed record ContentPieceRequest(
    Guid? SeoOpportunityId,
    string Title,
    string ContentType,
    string PrimaryKeyword,
    string SearchIntent,
    string Hypothesis,
    string BaselineSummary,
    string Objective,
    string Owner,
    string Brief,
    string DraftMarkdown,
    string MetaTitle,
    string MetaDescription,
    DateTimeOffset? ScheduledFor);

public sealed record EditorialTransitionRequest(string TargetStatus, string? Note, DateTimeOffset? ScheduledFor);
public sealed record ContentMeasurementRequest(int Impressions, int Clicks, string Notes, DateTimeOffset? MeasuredAt);

public sealed record EditorialHistoryResponse(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string Note,
    string Actor,
    DateTimeOffset ChangedAt)
{
    public static EditorialHistoryResponse FromEntity(EditorialHistory item) => new(
        item.Id, item.FromStatus, item.ToStatus, item.Note, item.Actor, item.ChangedAt);
}

public sealed record ContentPieceResponse(
    Guid Id,
    Guid ProjectId,
    Guid? SeoOpportunityId,
    string Title,
    string Slug,
    string ContentType,
    string PrimaryKeyword,
    string SearchIntent,
    string Hypothesis,
    string BaselineSummary,
    string Objective,
    string Owner,
    string Brief,
    string DraftMarkdown,
    string MetaTitle,
    string MetaDescription,
    string Status,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? MeasuredAt,
    int? ResultImpressions,
    int? ResultClicks,
    string ResultNotes,
    string WordPressEditUrl,
    int? WordPressPostId,
    string WordPressStatus,
    DateTimeOffset? WordPressDraftCreatedAt,
    bool IsDemoData,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<EditorialHistoryResponse> History)
{
    public static ContentPieceResponse FromEntity(ContentPiece item) => new(
        item.Id, item.ProjectId, item.SeoOpportunityId, item.Title, item.Slug, item.ContentType,
        item.PrimaryKeyword, item.SearchIntent, item.Hypothesis, item.BaselineSummary, item.Objective,
        item.Owner, item.Brief, item.DraftMarkdown, item.MetaTitle, item.MetaDescription, item.Status,
        item.ScheduledFor, item.MeasuredAt, item.ResultImpressions, item.ResultClicks, item.ResultNotes,
        item.SimulatedWordPressUrl, item.WordPressPostId, item.WordPressStatus, item.WordPressDraftCreatedAt,
        item.IsDemoData, item.CreatedAt, item.UpdatedAt,
        item.History.OrderByDescending(entry => entry.ChangedAt).Select(EditorialHistoryResponse.FromEntity).ToList());
}

public sealed record SeoSummaryResponse(
    int Opportunities,
    int ActivePieces,
    int PendingReview,
    int Scheduled,
    int Measured,
    bool ContainsDemoData);
