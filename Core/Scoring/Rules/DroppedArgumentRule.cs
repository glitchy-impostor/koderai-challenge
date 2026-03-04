using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Awards a bonus to the side that introduced a dropped argument.
/// A dropped argument is one the opponent failed to answer by the required speech —
/// in policy debate, silence concedes the argument.
///
/// Formula:
///   The side that introduced the dropped arg scores:
///     DroppedScore = argument.ComputedStrength × DroppedArgumentConcessionMultiplier
///
/// The multiplier (config-driven, default 1.5) reflects that an uncontested argument
/// is worth more than a contested one — it has "won" its thread without challenge.
///
/// Note: Conceded arguments (explicit status) are treated identically to Dropped.
/// </summary>
public class DroppedArgumentRule : IScoringRule
{
    public string RuleId      => "dropped-argument";
    public string DisplayName => "Dropped Arguments";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details = new List<ArgumentScoreDetail>();
        double affScore = 0, negScore = 0;
        var multiplier = context.Scoring.DroppedArgumentConcessionMultiplier;

        foreach (var node in context.Flow.Nodes.Values)
        {
            if (node.Status is not (ArgumentStatus.Dropped or ArgumentStatus.Conceded))
                continue;

            // The INTRODUCING side gets the concession bonus
            var score = node.ComputedStrength * multiplier;

            if (node.Side == Side.AFF) affScore += score;
            else                       negScore += score;

            var reason = node.StatusIsOverridden ? "conceded (manual)" : "dropped (unanswered)";

            details.Add(new ArgumentScoreDetail
            {
                ArgumentId       = node.ArgumentId,
                SpeechId         = node.SpeechId,
                StockIssueTag    = node.StockIssueTag,
                Side             = node.Side.ToString(),
                SpeakerId        = node.SpeakerId,
                RuleId           = RuleId,
                Score            = score,
                ComputedStrength = node.ComputedStrength,
                IsDropped        = true,
                DroppedPenalty   = score,
                Note             = $"Argument {reason} — {node.Side} scores " +
                                   $"{node.ComputedStrength:F2} × {multiplier} = {score:F2}"
            });
        }

        return new RuleResult
        {
            RuleId          = RuleId,
            DisplayName     = DisplayName,
            AffScore        = affScore,
            NegScore        = negScore,
            ArgumentDetails = details,
            Explanation     = BuildExplanation(affScore, negScore, details)
        };
    }

    private static string BuildExplanation(double aff, double neg, List<ArgumentScoreDetail> details)
    {
        if (details.Count == 0)
            return "Dropped Arguments: No arguments were dropped this round.";

        var dropped = details.Count;
        var affDrops = details.Count(d => d.Side == "AFF");
        var negDrops = details.Count(d => d.Side == "NEG");
        return $"Dropped Arguments: {dropped} argument(s) went unanswered. " +
               $"AFF introduced {affDrops} dropped arg(s) (+{aff:F2}); " +
               $"NEG introduced {negDrops} dropped arg(s) (+{neg:F2}).";
    }
}
