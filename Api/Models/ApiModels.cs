using DebateScoringEngine.Core.Domain.Models;

namespace DebateScoringEngine.Api.Models;

/// <summary>Request body for POST /api/debate/score</summary>
public class ScoreDebateRequest
{
    /// <summary>The structured debate to score.</summary>
    public required Debate Debate { get; init; }

    /// <summary>
    /// If true, the response includes the full ExplanationGenerator output.
    /// If false (default), only the structured ScoringResult is returned.
    /// </summary>
    public bool IncludeFullExplanation { get; init; } = true;
}

/// <summary>Request body for POST /api/debate/enrich</summary>
public class EnrichDebateRequest
{
    public required Debate Debate { get; init; }

    /// <summary>
    /// User-supplied API key for the LLM provider.
    /// Never stored — used for this request only.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>Override the configured provider for this request ("Anthropic" or "OpenAI").</summary>
    public string? ProviderOverride { get; init; }
}

/// <summary>Response from POST /api/debate/enrich</summary>
public class EnrichDebateResponse
{
    /// <summary>The debate with enrichment fields filled in by the LLM.</summary>
    public required Debate EnrichedDebate { get; init; }

    /// <summary>How many argument fields were filled vs left null.</summary>
    public int FieldsFilled { get; init; }

    /// <summary>Arguments the LLM could not enrich (returned with original nulls).</summary>
    public List<string> SkippedArgumentIds { get; init; } = new();

    public string? Warning { get; init; }
}

/// <summary>Generic API error response.</summary>
public class ApiError
{
    public required string Error { get; init; }
    public List<string> Details { get; init; } = new();
}
