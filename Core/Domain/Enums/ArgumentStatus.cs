namespace DebateScoringEngine.Core.Domain.Enums;

/// <summary>
/// The lifecycle status of an argument within the debate flow.
/// Derived automatically by the engine unless overridden via enrichment.
/// </summary>
public enum ArgumentStatus
{
    /// <summary>Argument is live — responded to and still contested.</summary>
    Active,

    /// <summary>
    /// No response was made by the required speech. 
    /// Treated as conceded per policy debate rules.
    /// </summary>
    Dropped,

    /// <summary>Explicitly conceded by the opposing side or overridden by human/LLM.</summary>
    Conceded,

    /// <summary>Argument was picked up and re-asserted in a later speech.</summary>
    Extended
}
