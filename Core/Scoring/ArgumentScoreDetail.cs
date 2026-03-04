namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Per-argument score breakdown produced by a single rule.
/// Collected across all rules and aggregated into the final ScoringResult.
/// This is the most granular unit of scoring output — drives the Detail view in the UI.
/// </summary>
public class ArgumentScoreDetail
{
    public required string ArgumentId    { get; init; }
    public required string SpeechId      { get; init; }
    public required string StockIssueTag { get; init; }
    public required string Side          { get; init; }
    public required string RuleId        { get; init; }

    /// <summary>Speaker who made this argument. Null for side-level entries (prep time, etc.).</summary>
    public string? SpeakerId { get; init; }

    /// <summary>Net score contribution of this argument under this rule (can be negative for penalties).</summary>
    public double Score { get; init; }

    /// <summary>Alias for Score — matches the frontend's expected field name.</summary>
    public double NetScore => Score;

    /// <summary>The argument's overall computed strength (0–5), from the flow graph node.</summary>
    public double ComputedStrength { get; init; }

    /// <summary>Whether this argument was detected as dropped.</summary>
    public bool IsDropped { get; init; }

    /// <summary>Score penalty from being dropped (populated by DroppedArgumentRule).</summary>
    public double DroppedPenalty { get; set; }

    /// <summary>Score penalty from detected fallacies (populated by LogicalConsistencyRule).</summary>
    public double FallacyPenalty { get; set; }

    /// <summary>
    /// Cross-rule breakdown for this argument. Populated by ScoringEngine after all rules run.
    /// Each entry shows one rule's contribution to this argument's score.
    /// </summary>
    public List<RuleScore> RuleBreakdown { get; set; } = new();

    /// <summary>Human-readable note explaining how this score was derived.</summary>
    public string Note { get; init; } = string.Empty;
}

/// <summary>
/// One rule's score contribution for a single argument.
/// Used in the expanded argument detail view in the UI.
/// </summary>
public class RuleScore
{
    public string RuleName { get; init; } = string.Empty;
    public double Score    { get; init; }
    public string Notes    { get; init; } = string.Empty;
}
