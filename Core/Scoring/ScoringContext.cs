using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.Domain.Models;
using FlowGraphNS = DebateScoringEngine.Core.FlowGraph;

namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Shared read-only context passed to every scoring rule.
/// Rules must not mutate any of these objects — they only read and produce RuleResults.
/// This guarantees rules are stateless and independently testable.
/// </summary>
public class ScoringContext
{
    /// <summary>Fully built and enriched flow graph — central data structure.</summary>
    public required FlowGraphNS.FlowGraph Flow { get; init; }

    /// <summary>Format definition — speech order, stock issues, drop rules, hard gates.</summary>
    public required FormatConfig Format { get; init; }

    /// <summary>Scoring weights, penalties, and multipliers.</summary>
    public required ScoringConfig Scoring { get; init; }

    /// <summary>Round-specific config — motion and stock case library.</summary>
    public required RoundConfig Round { get; init; }

    /// <summary>The original debate — used by time/prep rules that need raw speech data.</summary>
    public required Debate Debate { get; init; }
}
