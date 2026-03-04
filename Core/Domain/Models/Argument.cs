using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// A single argument made during the debate.
/// The central unit of the flow graph — every speech is composed of arguments.
/// </summary>
public class Argument
{
    public required string ArgumentId { get; init; }
    public required string SpeechId { get; init; }
    public required string SpeakerId { get; init; }
    public required Side Side { get; init; }

    /// <summary>Maps to a stock issue defined in format-config.json.</summary>
    public required string StockIssueTag { get; init; }

    /// <summary>
    /// Optional reference to a stock case blueprint in round-config.json.
    /// When set, blueprint's defaultEnrichment fills null enrichment fields.
    /// </summary>
    public string? StockCaseId { get; init; }

    /// <summary>
    /// IDs of arguments this argument is responding to.
    /// Empty for original constructive arguments.
    /// Populated for rebuttals — creates edges in the flow graph.
    /// </summary>
    public List<string> RebuttalTargetIds { get; init; } = new();

    public required ArgumentCore Core { get; init; }
    public ArgumentEnrichment Enrichment { get; init; } = new();
}
