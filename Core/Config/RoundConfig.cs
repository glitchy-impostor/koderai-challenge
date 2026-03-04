namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Round-specific configuration. Set up before each round.
/// Contains the motion being debated and the active stock case library.
/// Loaded from round-config.json.
/// </summary>
public class RoundConfig
{
    public required string RoundId { get; init; }

    /// <summary>The full motion/resolution text being debated.</summary>
    public required string Motion { get; init; }

    /// <summary>Links to the format configuration to use for this round.</summary>
    public required string FormatId { get; init; }

    /// <summary>
    /// Combined list of system-shipped and user-defined stock case blueprints active for this round.
    /// System blueprints are pre-loaded from StockCaseLibrary/ and merged here.
    /// </summary>
    public List<StockCase> StockCaseLibrary { get; init; } = new();

    /// <summary>
    /// User-defined stock cases added via the Settings UI.
    /// Stored separately so they can be managed independently from system blueprints.
    /// </summary>
    public List<StockCase> UserStockCases { get; init; } = new();

    /// <summary>
    /// Returns a stock case by ID, searching both system and user cases.
    /// Returns null if not found.
    /// </summary>
    public StockCase? FindBlueprint(string stockCaseId) =>
        StockCaseLibrary.FirstOrDefault(sc => sc.StockCaseId == stockCaseId)
        ?? UserStockCases.FirstOrDefault(sc => sc.StockCaseId == stockCaseId);

    /// <summary>Returns all blueprints for a given stock issue tag.</summary>
    public IEnumerable<StockCase> GetBlueprintsForIssue(string issueTag) =>
        StockCaseLibrary.Concat(UserStockCases)
                        .Where(sc => sc.StockIssueTag == issueTag);
}
