namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Defines a single speech slot in the debate's speech order.
/// Used to build the ordered speech index and validate drop rules.
/// </summary>
public class SpeechDefinition
{
    public required string SpeechId { get; init; }

    /// <summary>"AFF", "NEG", or "CX"</summary>
    public required string Side { get; init; }

    /// <summary>"Constructive", "Rebuttal", or "CrossEx"</summary>
    public required string Type { get; init; }

    public int TimeSeconds { get; init; }
}
