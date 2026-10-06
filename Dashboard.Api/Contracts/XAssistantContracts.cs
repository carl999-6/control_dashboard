using Dashboard.Api.Domain;

namespace Dashboard.Api.Contracts;

public sealed record XAssistantSettingsRequest(
    bool IsEnabled,
    bool ApiReadEnabled,
    string SearchQuery,
    string Language,
    int MaximumPostsPerSync,
    decimal ReadCostUsdPerPost,
    string ToneInstructions,
    string LandingPath,
    string UtmCampaign);

public sealed record XAssistantSettingsResponse(
    bool IsEnabled,
    bool ApiReadEnabled,
    bool BearerTokenConfigured,
    string SearchQuery,
    string Language,
    int MaximumPostsPerSync,
    decimal ReadCostUsdPerPost,
    decimal MaximumSyncCostUsd,
    string ToneInstructions,
    string LandingPath,
    string UtmCampaign,
    DateTimeOffset? LastSyncAt,
    string LastError);

public sealed record XSourcePostRequest(
    string Url,
    string AuthorUsername,
    string Text,
    string Language,
    int LikeCount,
    int ReplyCount,
    int RepostCount,
    int QuoteCount,
    int ImpressionCount,
    DateTimeOffset? PostedAt);

public sealed record XSourcePostResponse(
    Guid Id,
    string ExternalPostId,
    string Url,
    string AuthorUsername,
    string Text,
    string Language,
    int LikeCount,
    int ReplyCount,
    int RepostCount,
    int QuoteCount,
    int ImpressionCount,
    string Status,
    string DataSource,
    DateTimeOffset PostedAt,
    DateTimeOffset ImportedAt)
{
    public static XSourcePostResponse FromEntity(XSourcePost item) => new(
        item.Id, item.ExternalPostId, item.Url, item.AuthorUsername, item.Text, item.Language,
        item.LikeCount, item.ReplyCount, item.RepostCount, item.QuoteCount, item.ImpressionCount,
        item.Status, item.DataSource, item.PostedAt, item.ImportedAt);
}

public sealed record XReplyProposalResponse(
    Guid Id,
    Guid XSourcePostId,
    string SourceUrl,
    string SourceAuthor,
    string SourceText,
    string RecommendedReply,
    string AlternativeOne,
    string AlternativeTwo,
    string SelectedReply,
    string Rationale,
    string RiskNotes,
    string Status,
    string PublishedReplyUrl,
    string TrackingUrl,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static XReplyProposalResponse FromEntity(XReplyProposal item) => new(
        item.Id, item.XSourcePostId, item.SourcePost.Url, item.SourcePost.AuthorUsername, item.SourcePost.Text,
        item.RecommendedReply, item.AlternativeOne, item.AlternativeTwo, item.SelectedReply,
        item.Rationale, item.RiskNotes, item.Status, item.PublishedReplyUrl, item.TrackingUrl, item.PublishedAt,
        item.CreatedAt, item.UpdatedAt);
}

public sealed record XProposalTransitionRequest(string TargetStatus, string? SelectedReply, string? PublishedReplyUrl);

public sealed record XAssistantRunResult(
    bool Succeeded,
    string ErrorCode,
    string ErrorMessage,
    Guid? ProposalId,
    string SourceUrl,
    string Model,
    int InputTokens,
    int OutputTokens,
    int ImportedPosts,
    decimal EstimatedXCostUsd,
    string RecommendedReply,
    string AlternativeOne,
    string AlternativeTwo,
    string TrackingUrl);

public sealed record XSyncResult(
    bool Succeeded,
    string ErrorCode,
    string ErrorMessage,
    int ReadPosts,
    int ImportedPosts,
    decimal EstimatedCostUsd);
