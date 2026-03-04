namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Defines a single stock issue within the debate format.
/// E.g., Topicality, Harms, Solvency.
/// </summary>
public class StockIssueDefinition
{
    public required string Id { get; init; }
    public required string Label { get; init; }

    /// <summary>
    /// Which side bears the burden of proof for this issue.
    /// E.g., "AFF" must prove Harms; "NEG" typically raises Topicality.
    /// </summary>
    public required string ObligatedSide { get; init; }
}
