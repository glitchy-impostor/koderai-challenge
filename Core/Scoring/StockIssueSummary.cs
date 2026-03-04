using System.Text.Json.Serialization;

namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Rolled-up score for a single stock issue across all rules.
/// Drives the Summary view in the UI (default display).
/// </summary>
public class StockIssueSummary
{
    public required string IssueId    { get; init; }
    public required string IssueLabel { get; init; }

    public double AffRawScore  { get; init; }
    public double NegRawScore  { get; init; }
    public double AffWeighted  { get; init; }
    public double NegWeighted  { get; init; }
    public double Weight       { get; init; }

    /// <summary>Alias for AffWeighted — matches the frontend's expected field name.</summary>
    [JsonPropertyName("affWeightedScore")]
    public double AffWeightedScore => AffWeighted;

    /// <summary>Alias for NegWeighted — matches the frontend's expected field name.</summary>
    [JsonPropertyName("negWeightedScore")]
    public double NegWeightedScore => NegWeighted;

    /// <summary>Which side won this issue, or null if it's a hard gate issue (decided separately).</summary>
    public string? IssueWinner { get; init; }

    /// <summary>Alias for IssueWinner — matches the frontend's expected field name.</summary>
    [JsonPropertyName("winner")]
    public string? Winner => IssueWinner;

    public bool IsHardGate     { get; init; }
    public string Notes        { get; init; } = string.Empty;
}
