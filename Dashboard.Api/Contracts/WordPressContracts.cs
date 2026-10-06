namespace Dashboard.Api.Contracts;

public sealed record WordPressSettingsRequest(bool IsEnabled, string BaseUrl, string Username);

public sealed record WordPressSettingsResponse(
    bool IsEnabled,
    bool PasswordConfigured,
    string BaseUrl,
    string Username,
    string Status,
    DateTimeOffset? LastCheckedAt,
    string LastError);

public sealed record WordPressConnectionTestResponse(bool Succeeded, string Status, string Message);

public sealed record WordPressDraftResult(
    bool Succeeded,
    string ErrorCode,
    string ErrorMessage,
    Guid ContentPieceId,
    int? PostId,
    string EditUrl,
    bool AlreadyExisted);
