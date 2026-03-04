namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Defines when an argument becomes "dropped" (conceded by silence).
/// If an argument introduced in <see cref="ArgumentIntroducedIn"/> 
/// has no rebuttal by <see cref="MustBeAnsweredBy"/>, it is marked Dropped.
/// </summary>
public class DropRule
{
    public required string ArgumentIntroducedIn { get; init; }
    public required string MustBeAnsweredBy { get; init; }
}
