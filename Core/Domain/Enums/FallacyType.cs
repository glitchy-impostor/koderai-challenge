namespace DebateScoringEngine.Core.Domain.Enums;

/// <summary>
/// Logical fallacy types that can be tagged on an argument.
/// Each maps to a score penalty in scoring-config.json.
/// </summary>
public enum FallacyType
{
    StrawMan,
    AdHominem,
    FalseDichotomy,
    SlipperySlope,
    AppealToAuthority,
    Repetition
}
