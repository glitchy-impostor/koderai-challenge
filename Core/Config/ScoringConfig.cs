namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Defines all scoring weights, multipliers, and penalties.
/// Reusable across rounds. Loaded from scoring-config.json.
/// All values are used directly by scoring rules — no hard-coded numbers anywhere in engine code.
/// </summary>
public class ScoringConfig
{
    /// <summary>Weight per stock issue. Must sum to 1.0 (excluding hard gate issues).</summary>
    public required Dictionary<string, double> StockIssueWeights { get; init; }

    /// <summary>
    /// Ordered list of stock issue IDs used to break ties.
    /// First issue where one side leads determines the winner.
    /// </summary>
    public List<string> TiebreakerPriority { get; init; } = new();

    /// <summary>Score multiplier based on evidence quality.</summary>
    public required Dictionary<string, double> EvidenceQualityMultipliers { get; init; }

    /// <summary>Base impact score by magnitude tier.</summary>
    public required Dictionary<string, double> ImpactMagnitudeScores { get; init; }

    /// <summary>Score penalty subtracted per fallacy type.</summary>
    public required Dictionary<string, double> FallacyPenalties { get; init; }

    /// <summary>
    /// Multiplier applied to the score that a dropped argument awards the other side.
    /// E.g., 1.5 means a dropped argument is worth 50% more than a normal one.
    /// </summary>
    public double DroppedArgumentConcessionMultiplier { get; init; } = 1.5;

    /// <summary>Score bonus awarded to examiner per admission extracted in CX.</summary>
    public double CxAdmissionBonus { get; init; } = 0.3;

    /// <summary>Score penalty applied to respondent per evasive answer in CX.</summary>
    public double CxEvasionPenalty { get; init; } = 0.2;

    public TimeEfficiencyConfig TimeEfficiency { get; init; } = new();
    public PrepTimeConfig PrepTimeEfficiency { get; init; } = new();

    /// <summary>
    /// Fallback argument strength when enrichment.argumentStrength is null
    /// AND it cannot be computed from impact/evidence (both also null after blueprint fallback).
    /// </summary>
    public double DefaultArgumentStrengthWhenAbsent { get; init; } = 2.5;

    /// <summary>
    /// Weight applied when scoring how effectively a rebuttal addresses its target.
    /// Scales the rebuttal effectiveness score relative to argument strength scores.
    /// </summary>
    public double RebuttalEffectivenessWeight { get; init; } = 0.4;

    // ── Global defaults used when enrichment + blueprint both have null fields ──

    public string DefaultEvidenceQuality { get; init; } = "Unverified";
    public string DefaultImpactMagnitude { get; init; } = "Minor";

    // ── Convenience lookups (safe-get with defaults) ──

    public double GetEvidenceMultiplier(string quality) =>
        EvidenceQualityMultipliers.TryGetValue(quality, out var v) ? v : 0.25;

    public double GetImpactScore(string magnitude) =>
        ImpactMagnitudeScores.TryGetValue(magnitude, out var v) ? v : 2.0;

    public double GetFallacyPenalty(string fallacy) =>
        FallacyPenalties.TryGetValue(fallacy, out var v) ? v : 0.0;

    public double GetStockIssueWeight(string issueId) =>
        StockIssueWeights.TryGetValue(issueId, out var v) ? v : 0.0;
}

public class TimeEfficiencyConfig
{
    /// <summary>Score penalty per second over the allocated time.</summary>
    public double OverTimePenaltyPerSecond { get; init; } = 0.01;

    /// <summary>
    /// Threshold below which a speech is considered suspiciously short.
    /// E.g., 0.75 means using less than 75% of allocated time triggers a penalty.
    /// </summary>
    public double UnderTimeThresholdPercent { get; init; } = 0.75;

    /// <summary>Flat penalty applied when speech is under the threshold.</summary>
    public double UnderTimePenalty { get; init; } = 0.5;
}

public class PrepTimeConfig
{
    /// <summary>Tiny bonus per second of prep time NOT used (rewards efficiency).</summary>
    public double UnusedPrepBonusPerSecond { get; init; } = 0.001;

    /// <summary>Flat penalty for going over the allocated prep time budget.</summary>
    public double OverPrepPenalty { get; init; } = 2.0;
}
