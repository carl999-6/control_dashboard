namespace Dashboard.Api.Services;

public sealed record GeminiGeneration(string Text, int InputTokens, int OutputTokens, string FinishReason);

public interface IGeminiApiClient
{
    Task<GeminiGeneration> GenerateAsync(
        string apiKey,
        string model,
        string prompt,
        int maximumOutputTokens,
        CancellationToken cancellationToken);
}

public sealed class GeminiApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
