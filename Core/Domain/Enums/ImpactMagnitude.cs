namespace DebateScoringEngine.Core.Domain.Enums;

/// <summary>
/// Represents the scale of harm or benefit described in an argument's impact.
/// Maps to base scores in scoring-config.json.
/// </summary>
public enum ImpactMagnitude
{
    Existential,
    Catastrophic,
    Significant,
    Minor,
    Negligible
}
