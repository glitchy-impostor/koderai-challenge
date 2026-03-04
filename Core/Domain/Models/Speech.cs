using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// A single speech turn in the debate.
/// References argument IDs — actual argument objects live on the Debate.
/// </summary>
public class Speech
{
    public required string SpeechId { get; init; }
    public required string SpeakerId { get; init; }
    public required Side Side { get; init; }

    public int TimeAllocatedSeconds { get; init; }
    public int TimeUsedSeconds { get; init; }

    /// <summary>Ordered list of argument IDs presented in this speech.</summary>
    public List<string> ArgumentIds { get; init; } = new();
}
