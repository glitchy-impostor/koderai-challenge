namespace DebateScoringEngine.Api.Services;

/// <summary>
/// Minimal abstraction over an LLM API.
/// Only exposes what the enrichment service needs: a single chat completion call.
/// Provider-specific concerns (auth, retry, rate limiting) are handled in each implementation.
/// </summary>
public interface ILlmProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Sends a single user prompt and returns the model's text response.
    /// The caller is responsible for all prompt construction and response parsing.
    /// </summary>
    Task<LlmResponse> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string apiKey,
        CancellationToken cancellationToken = default);
}

public class LlmResponse
{
    public bool   Success       { get; init; }
    public string Content       { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public int    InputTokens   { get; init; }
    public int    OutputTokens  { get; init; }
}
