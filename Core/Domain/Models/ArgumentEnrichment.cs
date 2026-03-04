using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// Optional enrichment metadata for an argument.
/// Fields are nullable — null means "not yet provided."
/// Resolved at scoring time via the fallback hierarchy:
///   1. Explicit value here (highest trust)
///   2. Stock case blueprint default
///   3. Global scoring config default
/// </summary>
public class ArgumentEnrichment
{
    public EvidenceQuality? EvidenceQuality { get; set; }
    public ImpactMagnitude? ImpactMagnitude { get; set; }
    public List<FallacyType>? Fallacies { get; set; }

    /// <summary>
    /// Directly provided argument strength (0–5 scale).
    /// When null, the engine computes this from impact, evidence, and fallacy fields.
    /// </summary>
    public double? ArgumentStrength { get; set; }

    /// <summary>
    /// Explicit status override. When null, engine derives status from flow graph structure.
    /// </summary>
    public ArgumentStatus? Status { get; set; }
}
