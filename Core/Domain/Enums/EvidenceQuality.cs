namespace DebateScoringEngine.Core.Domain.Enums;

/// <summary>
/// Represents the quality/credibility tier of evidence backing an argument.
/// Maps to multipliers in scoring-config.json.
/// </summary>
public enum EvidenceQuality
{
    PeerReviewed,
    ExpertOpinion,
    NewsSource,
    Anecdotal,
    Unverified
}
