namespace DebateScoringEngine.Core.Config;

/// <summary>
/// A stock case blueprint — a pre-defined argument template for a given motion.
/// Arguments in the debate can reference a blueprint via StockCaseId to inherit
/// default enrichment values, improving scoring quality when explicit enrichment is absent.
/// 
/// "source: system" = ships with codebase in StockCaseLibrary/
/// "source: user"   = created by user in Settings UI, stored in round-config.json
/// </summary>
public class StockCase
{
    public required string StockCaseId { get; init; }
    public required string Label { get; init; }
    public required string StockIssueTag { get; init; }

    /// <summary>"AFF" or "NEG" — which side typically runs this argument.</summary>
    public required string Side { get; init; }

    /// <summary>"system" (shipped) or "user" (custom).</summary>
    public string Source { get; init; } = "user";

    /// <summary>
    /// Default enrichment values inherited by arguments referencing this blueprint.
    /// Only null fields in the argument's enrichment are filled from here.
    /// </summary>
    public StockCaseEnrichmentDefaults DefaultEnrichment { get; init; } = new();

    /// <summary>
    /// A blueprint argument showing the canonical form of this stock argument.
    /// Useful as a reference for human input and for the LLM enrichment prompt.
    /// </summary>
    public StockCaseBlueprintArgument? BlueprintArgument { get; init; }
}

public class StockCaseEnrichmentDefaults
{
    public string? EvidenceQuality { get; init; }
    public string? ImpactMagnitude { get; init; }
    public List<string> Fallacies { get; init; } = new();
}

public class StockCaseBlueprintArgument
{
    public string? Claim { get; init; }
    public string? Reasoning { get; init; }
    public string? Impact { get; init; }
}
