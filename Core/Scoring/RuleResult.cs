namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Output of a single scoring rule's evaluation of the entire debate.
/// Rules are additive — each produces independent AFF/NEG scores
/// that the ScoringEngine aggregates.
/// </summary>
public class RuleResult
{
    public required string RuleId       { get; init; }
    public required string DisplayName  { get; init; }

    /// <summary>Total score contribution to AFF from this rule across all arguments.</summary>
    public double AffScore { get; init; }

    /// <summary>Total score contribution to NEG from this rule across all arguments.</summary>
    public double NegScore { get; init; }

    /// <summary>Per-argument breakdown — feeds the Detail view.</summary>
    public List<ArgumentScoreDetail> ArgumentDetails { get; init; } = new();

    /// <summary>Human-readable explanation of what this rule evaluated and why.</summary>
    public string Explanation { get; init; } = string.Empty;
}
