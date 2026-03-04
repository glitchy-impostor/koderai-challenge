namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// The structural content of an argument — always required.
/// These fields define what the debater actually said.
/// No scoring happens on raw text; scoring uses enrichment fields.
/// </summary>
public class ArgumentCore
{
    public required string Claim { get; init; }
    public required string Reasoning { get; init; }
    public required string Impact { get; init; }
    public string? EvidenceSource { get; init; }
}
