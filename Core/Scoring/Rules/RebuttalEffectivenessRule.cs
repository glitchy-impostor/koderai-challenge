using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Scores how effectively each side's rebuttals addressed their targets.
///
/// Rationale: A strong rebuttal of a strong argument is worth more than
/// a strong rebuttal of a weak argument — this reflects real debate judging,
/// where "winning" a critical argument matters more than winning a minor one.
///
/// Formula per rebuttal edge (source rebuts target):
///   effectiveness = clamp(source.ComputedStrength / max(target.ComputedStrength, 0.01), 0, 1)
///   score = effectiveness × target.ComputedStrength × RebuttalEffectivenessWeight
///
///   effectiveness > 1 is clamped to 1 (a rebuttal can't be more than 100% effective)
///   The score scales with target strength: beating a 4.0 arg is worth more than a 1.0 arg.
///
/// Score is awarded to the SOURCE side (the rebuttal author).
/// </summary>
public class RebuttalEffectivenessRule : IScoringRule
{
    public string RuleId      => "rebuttal-effectiveness";
    public string DisplayName => "Rebuttal Effectiveness";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details = new List<ArgumentScoreDetail>();
        double affScore = 0, negScore = 0;
        var weight = context.Scoring.RebuttalEffectivenessWeight;

        foreach (var edge in context.Flow.Edges)
        {
            var source = context.Flow.GetNode(edge.SourceArgumentId);
            var target = context.Flow.GetNode(edge.TargetArgumentId);

            if (source == null || target == null) continue;

            var targetStrength  = Math.Max(target.ComputedStrength, 0.01);
            var sourceStrength  = source.ComputedStrength;
            var effectiveness   = Math.Min(sourceStrength / targetStrength, 1.0);
            var score           = effectiveness * targetStrength * weight;

            if (source.Side == Side.AFF) affScore += score;
            else                         negScore += score;

            details.Add(new ArgumentScoreDetail
            {
                ArgumentId       = source.ArgumentId,
                SpeechId         = source.SpeechId,
                StockIssueTag    = source.StockIssueTag,
                Side             = source.Side.ToString(),
                SpeakerId        = source.SpeakerId,
                RuleId           = RuleId,
                Score            = score,
                ComputedStrength = source.ComputedStrength,
                IsDropped        = source.Status is ArgumentStatus.Dropped or ArgumentStatus.Conceded,
                Note             = $"Rebutted {target.ArgumentId} " +
                                   $"(target strength {targetStrength:F2}, " +
                                   $"effectiveness {effectiveness:P0}) → {score:F2}"
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
            return "Rebuttal Effectiveness: No rebuttals were made this round.";

        var affRebuttals = details.Count(d => d.Side == "AFF");
        var negRebuttals = details.Count(d => d.Side == "NEG");
        return $"Rebuttal Effectiveness: {details.Count} rebuttal(s) scored. " +
               $"AFF made {affRebuttals} rebuttal(s) scoring {aff:F2}; " +
               $"NEG made {negRebuttals} rebuttal(s) scoring {neg:F2}.";
    }
}
