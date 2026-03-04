using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.Domain.Models;

namespace DebateScoringEngine.Tests.Helpers;

/// <summary>
/// Factory helpers for building minimal test debates and configs.
/// Keeps test code concise — only specify what matters for the test.
/// </summary>
public static class DebateFactory
{
    public static FormatConfig StandardFormat() => new()
    {
        FormatId   = "test-policy",
        FormatName = "Test Policy",
        StockIssues = new()
        {
            new() { Id = "Topicality", Label = "Topicality", ObligatedSide = "AFF" },
            new() { Id = "Harms",      Label = "Harms",      ObligatedSide = "AFF" },
            new() { Id = "Solvency",   Label = "Solvency",   ObligatedSide = "AFF" },
        },
        HardGateIssues = new() { "Topicality" },
        SpeechOrder = new()
        {
            new() { SpeechId = "1AC",  Side = "AFF", Type = "Constructive", TimeSeconds = 480 },
            new() { SpeechId = "CX-1", Side = "CX",  Type = "CrossEx",      TimeSeconds = 180 },
            new() { SpeechId = "1NC",  Side = "NEG", Type = "Constructive", TimeSeconds = 480 },
            new() { SpeechId = "CX-2", Side = "CX",  Type = "CrossEx",      TimeSeconds = 180 },
            new() { SpeechId = "2AC",  Side = "AFF", Type = "Constructive", TimeSeconds = 480 },
            new() { SpeechId = "CX-3", Side = "CX",  Type = "CrossEx",      TimeSeconds = 180 },
            new() { SpeechId = "2NC",  Side = "NEG", Type = "Constructive", TimeSeconds = 480 },
            new() { SpeechId = "1NR",  Side = "NEG", Type = "Rebuttal",     TimeSeconds = 300 },
            new() { SpeechId = "1AR",  Side = "AFF", Type = "Rebuttal",     TimeSeconds = 300 },
            new() { SpeechId = "2NR",  Side = "NEG", Type = "Rebuttal",     TimeSeconds = 300 },
            new() { SpeechId = "2AR",  Side = "AFF", Type = "Rebuttal",     TimeSeconds = 300 },
        },
        DropRules = new()
        {
            new() { ArgumentIntroducedIn = "1AC", MustBeAnsweredBy = "1NC" },
            new() { ArgumentIntroducedIn = "1NC", MustBeAnsweredBy = "2AC" },
            new() { ArgumentIntroducedIn = "2AC", MustBeAnsweredBy = "2NC" },
            new() { ArgumentIntroducedIn = "2NC", MustBeAnsweredBy = "1AR" },
            new() { ArgumentIntroducedIn = "1NR", MustBeAnsweredBy = "2AR" },
            new() { ArgumentIntroducedIn = "1AR", MustBeAnsweredBy = "2NR" },
            new() { ArgumentIntroducedIn = "2NR", MustBeAnsweredBy = "2AR" },
        },
        PrepTimeSeconds = new() { ["AFF"] = 480, ["NEG"] = 480 },
        CoreArgumentFields = new() { "claim", "reasoning", "impact" }
    };

    public static ScoringConfig StandardScoring() => new()
    {
        StockIssueWeights = new()
        {
            ["Topicality"] = 0.0,
            ["Harms"]      = 0.50,
            ["Solvency"]   = 0.50,
        },
        TiebreakerPriority = new() { "Solvency", "Harms" },
        EvidenceQualityMultipliers = new()
        {
            ["PeerReviewed"]  = 1.00,
            ["ExpertOpinion"] = 0.85,
            ["NewsSource"]    = 0.70,
            ["Anecdotal"]     = 0.40,
            ["Unverified"]    = 0.25,
        },
        ImpactMagnitudeScores = new()
        {
            ["Existential"]  = 5.0,
            ["Catastrophic"] = 4.0,
            ["Significant"]  = 3.0,
            ["Minor"]        = 2.0,
            ["Negligible"]   = 1.0,
        },
        FallacyPenalties = new()
        {
            ["StrawMan"]   = 0.50,
            ["AdHominem"]  = 0.75,
            ["Repetition"] = 0.30,
        },
        DefaultEvidenceQuality = "Unverified",
        DefaultImpactMagnitude = "Minor",
        DroppedArgumentConcessionMultiplier = 1.5,
        RebuttalEffectivenessWeight = 0.4,
        DefaultArgumentStrengthWhenAbsent = 2.5,
    };

    public static RoundConfig EmptyRound() => new()
    {
        RoundId  = "test-round",
        Motion   = "Test motion",
        FormatId = "test-policy",
    };

    public static RoundConfig RoundWithBlueprint(StockCase blueprint) => new()
    {
        RoundId          = "test-round",
        Motion           = "Test motion",
        FormatId         = "test-policy",
        StockCaseLibrary = new() { blueprint },
    };

    public static Argument Arg(
        string id,
        string speechId,
        Side side,
        string issue = "Harms",
        string[]? rebuttalTargets = null,
        string? stockCaseId = null,
        EvidenceQuality? evidence = null,
        ImpactMagnitude? impact = null,
        List<FallacyType>? fallacies = null,
        ArgumentStatus? status = null,
        double? strength = null) => new()
    {
        ArgumentId        = id,
        SpeechId          = speechId,
        SpeakerId         = side == Side.AFF ? "spk-aff" : "spk-neg",
        Side              = side,
        StockIssueTag     = issue,
        StockCaseId       = stockCaseId,
        RebuttalTargetIds = rebuttalTargets?.ToList() ?? new(),
        Core = new()
        {
            Claim     = $"Claim of {id}",
            Reasoning = $"Reasoning of {id}",
            Impact    = $"Impact of {id}",
        },
        Enrichment = new()
        {
            EvidenceQuality  = evidence,
            ImpactMagnitude  = impact,
            Fallacies        = fallacies,
            Status           = status,
            ArgumentStrength = strength,
        }
    };

    public static Debate Debate(params Argument[] arguments) => new()
    {
        DebateId  = "test-debate",
        RoundId   = "test-round",
        Teams     = new()
        {
            ["AFF"] = new() { TeamId = "aff", Side = Side.AFF, SpeakerIds = new() { "spk-aff" } },
            ["NEG"] = new() { TeamId = "neg", Side = Side.NEG, SpeakerIds = new() { "spk-neg" } },
        },
        Speakers  = new()
        {
            new() { SpeakerId = "spk-aff", Name = "AFF Speaker", Side = Side.AFF },
            new() { SpeakerId = "spk-neg", Name = "NEG Speaker", Side = Side.NEG },
        },
        Speeches = arguments
            .GroupBy(a => a.SpeechId)
            .Select(g => new Speech
            {
                SpeechId             = g.Key,
                SpeakerId            = g.First().SpeakerId,
                Side                 = g.First().Side,
                TimeAllocatedSeconds = 480,
                TimeUsedSeconds      = 460,
                ArgumentIds          = g.Select(a => a.ArgumentId).ToList(),
            })
            .ToList(),
        Arguments           = arguments.ToDictionary(a => a.ArgumentId),
        PrepTimeUsedSeconds = new() { ["AFF"] = 200, ["NEG"] = 250 },
    };
}
