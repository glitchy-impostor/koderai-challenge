using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.Domain.Models;

namespace DebateScoringEngine.Core.FlowGraph;

/// <summary>
/// A node in the flow graph representing a single argument.
/// Wraps the raw Argument domain model and adds derived/computed state
/// that the scoring engine needs — status, resolved enrichment, computed strength.
///
/// Computed fields are populated by FlowGraphBuilder after construction.
/// Scoring rules treat these as read-only inputs.
/// </summary>
public class ArgumentNode
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public string ArgumentId => Argument.ArgumentId;
    public string SpeechId   => Argument.SpeechId;
    public string SpeakerId  => Argument.SpeakerId;
    public Side   Side       => Argument.Side;
    public string StockIssueTag => Argument.StockIssueTag;

    /// <summary>The original argument from the debate input.</summary>
    public Argument Argument { get; }

    // ── Derived status ────────────────────────────────────────────────────────

    /// <summary>
    /// Final resolved status of this argument.
    /// Priority: explicit enrichment override → engine-derived (drop detection).
    /// Set by FlowGraphBuilder after all edges and drop rules are processed.
    /// </summary>
    public ArgumentStatus Status { get; internal set; } = ArgumentStatus.Active;

    /// <summary>True if this argument's status was set by explicit enrichment, not derived.</summary>
    public bool StatusIsOverridden { get; internal set; }

    // ── Resolved enrichment ───────────────────────────────────────────────────

    /// <summary>
    /// Enrichment values resolved via the three-tier fallback hierarchy:
    ///   1. Explicit enrichment on the argument (highest trust)
    ///   2. Stock case blueprint defaults (if stockCaseId matches)
    ///   3. Global scoring-config defaults (always present)
    ///
    /// Populated by FlowGraphBuilder. Scoring rules read from here — never
    /// directly from Argument.Enrichment, ensuring the fallback is always applied.
    /// </summary>
    public ResolvedEnrichment Resolved { get; internal set; } = new();

    // ── Computed strength ─────────────────────────────────────────────────────

    /// <summary>
    /// Final computed argument strength (0–5 scale).
    /// If enrichment.ArgumentStrength is explicitly provided, that value is used directly.
    /// Otherwise computed from: (ImpactScore × EvidenceMultiplier) − FallacyPenalties.
    /// Set by FlowGraphBuilder using ScoringConfig values.
    /// </summary>
    public double ComputedStrength { get; internal set; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public ArgumentNode(Argument argument)
    {
        Argument = argument;
    }

    public override string ToString() =>
        $"[{Side} | {SpeechId} | {StockIssueTag} | {ArgumentId} | {Status} | Strength={ComputedStrength:F2}]";
}

/// <summary>
/// Fully resolved enrichment values for an argument node.
/// All fields are non-nullable — the fallback hierarchy guarantees a value for everything.
/// </summary>
public class ResolvedEnrichment
{
    public EvidenceQuality EvidenceQuality { get; set; } = EvidenceQuality.Unverified;
    public ImpactMagnitude ImpactMagnitude { get; set; } = ImpactMagnitude.Minor;
    public List<FallacyType> Fallacies     { get; set; } = new();

    /// <summary>
    /// True if ArgumentStrength was explicitly provided and should be used directly.
    /// When false, ComputedStrength is derived from impact × evidence − fallacies.
    /// </summary>
    public bool StrengthExplicit { get; set; }
    public double? ExplicitStrength { get; set; }

    /// <summary>
    /// The source that provided these values — for explanation output.
    /// "explicit" | "blueprint:{stockCaseId}" | "default"
    /// </summary>
    public string EvidenceSource { get; set; } = "default";
    public string ImpactSource   { get; set; } = "default";
}
