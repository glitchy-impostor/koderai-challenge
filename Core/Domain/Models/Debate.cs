namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// The top-level debate input object. 
/// This is the structured JSON submitted to the engine.
/// References round-config.json via RoundId for motion and stock case library.
/// </summary>
public class Debate
{
    public required string DebateId { get; init; }

    /// <summary>Links to round-config.json for motion + stock case library.</summary>
    public required string RoundId { get; init; }

    public required Dictionary<string, Team> Teams { get; init; }
    public required List<Speaker> Speakers { get; init; }
    public required List<Speech> Speeches { get; init; }

    /// <summary>All arguments keyed by ArgumentId for O(1) lookup.</summary>
    public required Dictionary<string, Argument> Arguments { get; init; }

    public List<CrossExamination> CrossExaminations { get; init; } = new();

    /// <summary>Prep time consumed per side in seconds.</summary>
    public Dictionary<string, int> PrepTimeUsedSeconds { get; init; } = new();
}
