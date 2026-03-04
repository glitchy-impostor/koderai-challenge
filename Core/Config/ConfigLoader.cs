using DebateScoringEngine.Core.Domain.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DebateScoringEngine.Core.Config;

/// <summary>
/// Loads and deserializes all three configuration files.
/// Uses System.Text.Json — no external packages required.
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static FormatConfig LoadFormat(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<FormatConfig>(json, Options)
               ?? throw new InvalidOperationException($"Failed to deserialize FormatConfig from {path}");
    }

    public static ScoringConfig LoadScoring(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ScoringConfig>(json, Options)
               ?? throw new InvalidOperationException($"Failed to deserialize ScoringConfig from {path}");
    }

    public static RoundConfig LoadRound(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RoundConfig>(json, Options)
               ?? throw new InvalidOperationException($"Failed to deserialize RoundConfig from {path}");
    }

    public static Debate LoadDebate(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Debate>(json, Options)
               ?? throw new InvalidOperationException($"Failed to deserialize Debate from {path}");
    }

    /// <summary>
    /// Loads system stock case blueprints from a directory of JSON files.
    /// Each file contains an array of StockCase objects.
    /// Merged into the RoundConfig's StockCaseLibrary.
    /// </summary>
    public static List<StockCase> LoadStockCaseLibrary(string libraryDirectory)
    {
        if (!Directory.Exists(libraryDirectory))
            return new List<StockCase>();

        var cases = new List<StockCase>();
        foreach (var file in Directory.GetFiles(libraryDirectory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var batch = JsonSerializer.Deserialize<List<StockCase>>(json, Options);
            if (batch != null)
                cases.AddRange(batch);
        }
        return cases;
    }

    /// <summary>
    /// Serializes any object back to a JSON file (used by ConfigController for Settings edits).
    /// </summary>
    public static void Save<T>(T obj, string path)
    {
        var writeOptions = new JsonSerializerOptions(Options)
        {
            WriteIndented = true
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, writeOptions));
    }
}
