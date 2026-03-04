using DebateScoringEngine.Core.Config;

namespace DebateScoringEngine.Api.Services;

/// <summary>
/// Singleton service that owns all config file I/O.
/// Provides loaded configs to controllers and handles save-back from Settings edits.
///
/// Configs are loaded once at startup and re-read from disk on every GET
/// so edits made outside the app (e.g. direct file edit) are always reflected.
/// Writes are synchronous and atomic (write-then-replace).
///
/// Thread safety: reads are safe (immutable objects returned).
/// Concurrent writes are last-write-wins — acceptable for single-user local tool.
/// </summary>
public class ConfigService
{
    private readonly string _formatPath;
    private readonly string _scoringPath;
    private readonly string _roundPath;
    private readonly string _stockCaseLibDir;

    public ConfigService(IConfiguration configuration)
    {
        // Resolve paths relative to the app's content root
        var root = Directory.GetCurrentDirectory();
        var paths = configuration.GetSection("ConfigPaths");

        _formatPath      = ResolvePath(root, paths["FormatConfig"]  ?? "Config/format-config.json");
        _scoringPath     = ResolvePath(root, paths["ScoringConfig"] ?? "Config/scoring-config.json");
        _roundPath       = ResolvePath(root, paths["RoundConfig"]   ?? "Config/round-config.json");
        _stockCaseLibDir = ResolvePath(root, paths["StockCaseLibraryDir"] ?? "StockCaseLibrary");
    }

    // ── Format config ─────────────────────────────────────────────────────────

    public FormatConfig GetFormat() => ConfigLoader.LoadFormat(_formatPath);

    public void SaveFormat(FormatConfig config) => ConfigLoader.Save(config, _formatPath);

    // ── Scoring config ────────────────────────────────────────────────────────

    public ScoringConfig GetScoring() => ConfigLoader.LoadScoring(_scoringPath);

    public void SaveScoring(ScoringConfig config) => ConfigLoader.Save(config, _scoringPath);

    // ── Round config ──────────────────────────────────────────────────────────

    public RoundConfig GetRound()
    {
        var round = ConfigLoader.LoadRound(_roundPath);

        // Merge system stock cases from library dir into the round config
        // so controllers always get the full combined library
        if (Directory.Exists(_stockCaseLibDir))
        {
            var systemCases = ConfigLoader.LoadStockCaseLibrary(_stockCaseLibDir);
            var existingIds = round.StockCaseLibrary.Select(sc => sc.StockCaseId).ToHashSet();

            // Only add system cases not already explicitly listed in round-config.json
            var toAdd = systemCases.Where(sc => !existingIds.Contains(sc.StockCaseId)).ToList();

            return new RoundConfig
            {
                RoundId          = round.RoundId,
                Motion           = round.Motion,
                FormatId         = round.FormatId,
                StockCaseLibrary = round.StockCaseLibrary.Concat(toAdd).ToList(),
                UserStockCases   = round.UserStockCases,
            };
        }

        return round;
    }

    public void SaveRound(RoundConfig config) => ConfigLoader.Save(config, _roundPath);

    // ── Stock cases ───────────────────────────────────────────────────────────

    /// <summary>Returns all stock cases: system library + user-defined from round config.</summary>
    public List<StockCase> GetAllStockCases()
    {
        var round = GetRound();
        return round.StockCaseLibrary.Concat(round.UserStockCases).ToList();
    }

    /// <summary>
    /// Adds a user-defined stock case to the round config's userStockCases list.
    /// Rejects duplicates by stockCaseId.
    /// </summary>
    public (bool success, string? error) AddUserStockCase(StockCase stockCase)
    {
        var round = ConfigLoader.LoadRound(_roundPath); // load raw (without merged system cases)
        if (round.UserStockCases.Any(sc => sc.StockCaseId == stockCase.StockCaseId))
            return (false, $"Stock case '{stockCase.StockCaseId}' already exists.");

        var updated = new RoundConfig
        {
            RoundId          = round.RoundId,
            Motion           = round.Motion,
            FormatId         = round.FormatId,
            StockCaseLibrary = round.StockCaseLibrary,
            UserStockCases   = round.UserStockCases.Append(stockCase).ToList(),
        };

        ConfigLoader.Save(updated, _roundPath);
        return (true, null);
    }

    /// <summary>Removes a user-defined stock case by ID. Cannot remove system cases.</summary>
    public (bool success, string? error) DeleteUserStockCase(string stockCaseId)
    {
        var round = ConfigLoader.LoadRound(_roundPath);
        var existing = round.UserStockCases.FirstOrDefault(sc => sc.StockCaseId == stockCaseId);
        if (existing == null)
            return (false, $"User stock case '{stockCaseId}' not found. System cases cannot be deleted.");

        var updated = new RoundConfig
        {
            RoundId          = round.RoundId,
            Motion           = round.Motion,
            FormatId         = round.FormatId,
            StockCaseLibrary = round.StockCaseLibrary,
            UserStockCases   = round.UserStockCases.Where(sc => sc.StockCaseId != stockCaseId).ToList(),
        };

        ConfigLoader.Save(updated, _roundPath);
        return (true, null);
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    /// <summary>Validates that all config files are present and readable.</summary>
    public (bool ok, List<string> errors) ValidateConfigFiles()
    {
        var errors = new List<string>();
        foreach (var (label, path) in new[]
        {
            ("format-config.json",  _formatPath),
            ("scoring-config.json", _scoringPath),
            ("round-config.json",   _roundPath),
        })
        {
            if (!File.Exists(path))
                errors.Add($"{label} not found at: {path}");
        }
        return (errors.Count == 0, errors);
    }

    private static string ResolvePath(string root, string relativePath) =>
        Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(root, relativePath);
}
