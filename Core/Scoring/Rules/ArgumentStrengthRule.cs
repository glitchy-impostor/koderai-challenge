using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Scores each argument based on its computed strength.
/// ComputedStrength is already resolved from enrichment during FlowGraph construction,
/// so this rule simply reads and weights those values.
///
/// Score is attributed to the argument's side. Arguments that are Dropped or Conceded
/// do not contribute strength to their own side — DroppedArgumentRule handles their impact.
///
/// Formula per argument:
///   Active/Extended: side score += ComputedStrength
///   Dropped/Conceded: excluded (handled by DroppedArgumentRule)
/// </summary>
public class ArgumentStrengthRule : IScoringRule
{
    public string RuleId      => "argument-strength";
    public string DisplayName => "Argument Strength";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details = new List<ArgumentScoreDetail>();
        double affScore = 0, negScore = 0;

        foreach (var node in context.Flow.Nodes.Values)
        {
            // Dropped/Conceded arguments don't contribute their own strength here
            if (node.Status is ArgumentStatus.Dropped or ArgumentStatus.Conceded)
                continue;

            var score = node.ComputedStrength;

            if (node.Side == Side.AFF) affScore += score;
            else                       negScore += score;

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
                IsDropped        = false, // excluded above — active/extended only
                Note             = $"Strength {score:F2} " +
                                   $"(impact={node.Resolved.ImpactMagnitude}, " +
                                   $"evidence={node.Resolved.EvidenceQuality}, " +
                                   $"source={node.Resolved.EvidenceSource})"
            });
        }

        return new RuleResult
        {
            RuleId        = RuleId,
            DisplayName   = DisplayName,
            AffScore      = affScore,
            NegScore      = negScore,
            ArgumentDetails = details,
            Explanation   = BuildExplanation(affScore, negScore, details)
        };
    }

    private static string BuildExplanation(double aff, double neg, List<ArgumentScoreDetail> details)
    {
        var affCount = details.Count(d => d.Side == "AFF");
        var negCount = details.Count(d => d.Side == "NEG");
        return $"Argument Strength: AFF scored {aff:F2} across {affCount} active argument(s); " +
               $"NEG scored {neg:F2} across {negCount} active argument(s).";
    }
}
