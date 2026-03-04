namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Aggregated score breakdown for a single speaker across all rules.
/// Built by the ScoringEngine after all rules have run.
/// </summary>
public class SpeakerScoreSummary
{
    public required string SpeakerId { get; init; }
    public required string SpeakerName { get; init; }
    public required string Side { get; init; }

    /// <summary>Total net score across all rules for this speaker.</summary>
    public double TotalScore { get; init; }

    /// <summary>Number of arguments this speaker introduced.</summary>
    public int ArgumentCount { get; init; }

    /// <summary>Number of this speaker's arguments that were dropped by the opponent.</summary>
    public int DroppedCount { get; init; }

    /// <summary>Number of rebuttals this speaker made.</summary>
    public int RebuttalCount { get; init; }

    /// <summary>Average computed strength of this speaker's arguments.</summary>
    public double AverageStrength { get; init; }

    /// <summary>Per-rule score contributions for this speaker.</summary>
    public List<SpeakerRuleContribution> RuleContributions { get; init; } = new();
}

/// <summary>
/// One rule's total score contribution for a specific speaker.
/// </summary>
public class SpeakerRuleContribution
{
    public required string RuleId { get; init; }
    public required string DisplayName { get; init; }
    public double Score { get; init; }
    public int DetailCount { get; init; }
}
