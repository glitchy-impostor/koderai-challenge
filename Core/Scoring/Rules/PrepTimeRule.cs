using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Scores prep time usage efficiency per side.
///
/// Over prep: flat penalty for exceeding the total prep time budget.
///   Going over prep time is a rules violation — penalised regardless of how much over.
///
/// Unused prep: tiny per-second bonus for conserving prep time.
///   Finishing a round with prep time remaining suggests the team was well-prepared
///   and didn't need to scramble. The bonus is deliberately small so it never
///   dominates the score — it's a tiebreaker signal, not a primary scoring dimension.
///
/// Config:
///   unusedPrepBonusPerSecond — per second of unused prep (default 0.001)
///   overPrepPenalty — flat penalty for any overuse (default 2.0)
/// </summary>
public class PrepTimeRule : IScoringRule
{
    public string RuleId      => "prep-time";
    public string DisplayName => "Prep Time";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details = new List<ArgumentScoreDetail>();
        double affScore = 0, negScore = 0;
        var cfg = context.Scoring.PrepTimeEfficiency;

        foreach (var (sideKey, usedSeconds) in context.Debate.PrepTimeUsedSeconds)
        {
            if (!Enum.TryParse<Side>(sideKey, ignoreCase: true, out var side))
                continue;

            if (!context.Format.PrepTimeSeconds.TryGetValue(sideKey, out var allocated))
                continue;

            double score;
            string note;

            if (usedSeconds > allocated)
            {
                score = -cfg.OverPrepPenalty;
                note  = $"Over prep by {usedSeconds - allocated}s → {score:F2}";
            }
            else
            {
                var unused = allocated - usedSeconds;
                score = unused * cfg.UnusedPrepBonusPerSecond;
                note  = $"Unused prep: {unused}s → +{score:F3}";
            }

            if (side == Side.AFF) affScore += score;
            else                  negScore += score;

            details.Add(new ArgumentScoreDetail
            {
                ArgumentId    = $"prep:{sideKey}",
                SpeechId      = "PrepTime",
                StockIssueTag = "PrepTime",
                Side          = sideKey,
                RuleId        = RuleId,
                Score         = score,
                Note          = $"[{sideKey} prep] {note}"
            });
        }

        return new RuleResult
        {
            RuleId          = RuleId,
            DisplayName     = DisplayName,
            AffScore        = affScore,
            NegScore        = negScore,
            ArgumentDetails = details,
            Explanation     = BuildExplanation(affScore, negScore)
        };
    }

    private static string BuildExplanation(double aff, double neg)
    {
        return $"Prep Time: AFF prep score {aff:F3}; NEG prep score {neg:F3}.";
    }
}
