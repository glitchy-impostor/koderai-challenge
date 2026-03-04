namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Defines the structural rules of the debate format.
/// Reusable across rounds — does not contain round-specific content.
/// Loaded from format-config.json.
/// </summary>
public class FormatConfig
{
    public required string FormatId { get; init; }
    public required string FormatName { get; init; }

    public required List<StockIssueDefinition> StockIssues { get; init; }

    /// <summary>
    /// Issues where losing immediately decides the round winner, regardless of weighted scores.
    /// Checked before weighted scoring runs. Results still displayed for informational purposes.
    /// An empty list means purely weighted scoring with no hard gates.
    /// </summary>
    public List<string> HardGateIssues { get; init; } = new();

    /// <summary>Ordered list of speeches — order is critical for drop detection.</summary>
    public required List<SpeechDefinition> SpeechOrder { get; init; }

    /// <summary>Rules defining when an argument must be answered to avoid being dropped.</summary>
    public List<DropRule> DropRules { get; init; } = new();

    /// <summary>Prep time budget per side in seconds.</summary>
    public Dictionary<string, int> PrepTimeSeconds { get; init; } = new();

    /// <summary>
    /// The core argument fields required by this format.
    /// Used for schema validation on input. E.g., ["claim", "reasoning", "impact"].
    /// </summary>
    public List<string> CoreArgumentFields { get; init; } = new();

    /// <summary>Returns the 0-based index of a speech in the ordered speech list.</summary>
    public int GetSpeechIndex(string speechId)
    {
        var idx = SpeechOrder.FindIndex(s => s.SpeechId == speechId);
        return idx; // -1 if not found — callers should handle
    }

    /// <summary>Returns the SpeechDefinition for a given speechId, or null.</summary>
    public SpeechDefinition? GetSpeech(string speechId) =>
        SpeechOrder.FirstOrDefault(s => s.SpeechId == speechId);
}
