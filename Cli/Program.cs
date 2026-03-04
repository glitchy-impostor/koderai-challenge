using System.Text.Json;
using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Core.Output;
using DebateScoringEngine.Core.Scoring;

/// <summary>
/// CLI entry point for the Debate Scoring Engine.
///
/// Usage:
///   dotnet run -- --input debate.json [options]
///
/// Options:
///   --input  <path>     Path to debate JSON file (required)
///   --config <dir>      Directory containing config files (default: ./Config)
///   --output <path>     Write JSON result to file instead of stdout
///   --brief             Print brief explanation only (no full breakdown)
///   --validate          Validate debate JSON against format config, then exit
///   --explain           Print full human-readable explanation (default behaviour)
///   --json              Output raw ScoringResult as JSON
/// </summary>

var parsedArgs = ParseArgs(Environment.GetCommandLineArgs()[1..]);

if (!parsedArgs.TryGetValue("input", out var inputPath))
{
    PrintUsage();
    return 1;
}

var configDir   = parsedArgs.GetValueOrDefault("config", "./Config");
var outputPath  = parsedArgs.GetValueOrDefault("output",  null);
var mode        = parsedArgs.ContainsKey("json")     ? "json"
                : parsedArgs.ContainsKey("brief")    ? "brief"
                : "explain";
var validateOnly = parsedArgs.ContainsKey("validate");

// ── Load configs ──────────────────────────────────────────────────────────────

if (!File.Exists(inputPath!))
{
    Console.Error.WriteLine($"Error: debate input file not found: {inputPath}");
    return 1;
}

string formatPath  = Path.Combine(configDir, "format-config.json");
string scoringPath = Path.Combine(configDir, "scoring-config.json");
string roundPath   = Path.Combine(configDir, "round-config.json");

foreach (var (label, path) in new[] {
    ("format-config.json", formatPath),
    ("scoring-config.json", scoringPath),
    ("round-config.json", roundPath) })
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"Error: {label} not found at {path}");
        Console.Error.WriteLine($"  Use --config <dir> to specify the config directory.");
        return 1;
    }
}

FormatConfig  format;
ScoringConfig scoring;
RoundConfig   round;
DebateScoringEngine.Core.Domain.Models.Debate debate;

try
{
    format  = ConfigLoader.LoadFormat(formatPath);
    scoring = ConfigLoader.LoadScoring(scoringPath);
    round   = ConfigLoader.LoadRound(roundPath);
    debate  = ConfigLoader.LoadDebate(inputPath!);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error loading files: {ex.Message}");
    return 1;
}

// ── Validate mode ─────────────────────────────────────────────────────────────

if (validateOnly)
{
    var errors = ValidateDebate(debate, format);
    if (errors.Count == 0)
    {
        Console.WriteLine($"✓ Debate '{debate.DebateId}' is valid against format '{format.FormatId}'.");
        return 0;
    }

    Console.Error.WriteLine($"✗ Validation failed ({errors.Count} error(s)):");
    foreach (var e in errors)
        Console.Error.WriteLine($"  • {e}");
    return 1;
}

// ── Run pipeline ──────────────────────────────────────────────────────────────

Console.Error.WriteLine($"[1/3] Building flow graph for debate '{debate.DebateId}'...");
var builder = new FlowGraphBuilder(format, scoring, round);
FlowGraph flow;

try
{
    flow = builder.Build(debate);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error building flow graph: {ex.Message}");
    return 1;
}

var summary = flow.GetSummary();
Console.Error.WriteLine($"      {summary.TotalArguments} arguments, " +
                         $"{summary.TotalEdges} rebuttals, " +
                         $"{summary.DroppedArguments} dropped.");

Console.Error.WriteLine("[2/3] Scoring...");
var engine = new ScoringEngine();
DebateScoringEngine.Core.Scoring.ScoringResult result;

try
{
    result = engine.Score(flow, debate, format, scoring, round);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error during scoring: {ex.Message}");
    return 1;
}

Console.Error.WriteLine($"      Winner: {result.Winner}  " +
                         $"(AFF {result.AffTotalScore:F3} — NEG {result.NegTotalScore:F3})");

Console.Error.WriteLine("[3/3] Generating output...");

// ── Produce output ────────────────────────────────────────────────────────────

string output = mode switch
{
    "json"  => SerializeResult(result),
    "brief" => ExplanationGenerator.GenerateBrief(result, round),
    _       => ExplanationGenerator.GenerateFull(result, flow, format, round)
};

if (outputPath != null)
{
    File.WriteAllText(outputPath, output);
    Console.Error.WriteLine($"      Written to {outputPath}");
}
else
{
    Console.WriteLine(output);
}

return 0;

// ── Helpers ───────────────────────────────────────────────────────────────────

static Dictionary<string, string?> ParseArgs(string[] argv)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < argv.Length; i++)
    {
        var key = argv[i].TrimStart('-');
        if (i + 1 < argv.Length && !argv[i + 1].StartsWith('-'))
            result[key] = argv[++i];
        else
            result[key] = null;
    }
    return result;
}

static List<string> ValidateDebate(
    DebateScoringEngine.Core.Domain.Models.Debate debate,
    FormatConfig format)
{
    var errors = new List<string>();

    // All arguments must have required core fields
    foreach (var (id, arg) in debate.Arguments)
    {
        if (string.IsNullOrWhiteSpace(arg.Core.Claim))
            errors.Add($"Argument {id}: missing 'claim'");
        if (string.IsNullOrWhiteSpace(arg.Core.Reasoning))
            errors.Add($"Argument {id}: missing 'reasoning'");
        if (string.IsNullOrWhiteSpace(arg.Core.Impact))
            errors.Add($"Argument {id}: missing 'impact'");
    }

    // All rebuttal targets must exist
    foreach (var (id, arg) in debate.Arguments)
    {
        foreach (var targetId in arg.RebuttalTargetIds)
        {
            if (!debate.Arguments.ContainsKey(targetId))
                errors.Add($"Argument {id}: rebuttalTargetId '{targetId}' not found");
        }
    }

    // All speech argument IDs must exist in arguments dict
    foreach (var speech in debate.Speeches)
    {
        foreach (var argId in speech.ArgumentIds)
        {
            if (!debate.Arguments.ContainsKey(argId))
                errors.Add($"Speech {speech.SpeechId}: argumentId '{argId}' not found");
        }
    }

    // All stock issue tags must be defined in format
    var validIssues = format.StockIssues.Select(s => s.Id).ToHashSet();
    foreach (var (id, arg) in debate.Arguments)
    {
        if (!validIssues.Contains(arg.StockIssueTag))
            errors.Add($"Argument {id}: stockIssueTag '{arg.StockIssueTag}' not defined in format");
    }

    return errors;
}

static string SerializeResult(DebateScoringEngine.Core.Scoring.ScoringResult result)
{
    var opts = new JsonSerializerOptions { WriteIndented = true };
    return JsonSerializer.Serialize(result, opts);
}

static void PrintUsage()
{
    Console.Error.WriteLine(@"
Debate Scoring Engine CLI
─────────────────────────
Usage:
  dotnet run --project Cli -- --input <debate.json> [options]

Options:
  --input  <path>    Debate JSON file to score (required)
  --config <dir>     Config directory (default: ./Config)
  --output <path>    Write output to file instead of stdout
  --explain          Full human-readable explanation (default)
  --brief            Short summary explanation
  --json             Raw JSON output (ScoringResult)
  --validate         Validate debate JSON only, do not score

Examples:
  dotnet run --project Cli -- --input Samples/sample-debate.json
  dotnet run --project Cli -- --input Samples/sample-debate.json --brief
  dotnet run --project Cli -- --input Samples/sample-debate.json --json --output result.json
  dotnet run --project Cli -- --input Samples/sample-debate.json --validate
");
}
