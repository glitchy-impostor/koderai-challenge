using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Penalizes arguments that contain logical fallacies.
/// Fallacies are tagged in the enrichment layer (human or LLM).
/// Each fallacy type has a configurable penalty in scoring-config.json.
///
/// Formula per argument:
///   penalty = sum(fallacyPenalties[fallacy] for fallacy in argument.fallacies)
///   The penalty is subtracted from the INTRODUCING side's consistency score.
///
/// This rule is separate from ArgumentStrengthRule (which also deducts fallacies
/// from computed strength) because it operates at the rule level — enabling
/// the score breakdown to show "Logical Consistency" as an independent dimension.
/// The strength formula deducts them to affect the argument's base strength;
/// this rule reports them as a standalone consistency penalty for transparency.
///
/// Note on double-counting: By design, fallacies impact BOTH argument strength
/// (via the formula) AND logical consistency score. This is intentional — a fallacious
/// argument is both weaker AND reflects poor reasoning, which are distinct dimensions.
/// The weights in scoring-config.json should account for this.
/// </summary>
public class LogicalConsistencyRule : IScoringRule
{
    public string RuleId      => "logical-consistency";
    public string DisplayName => "Logical Consistency";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details = new List<ArgumentScoreDetail>();
        double affPenalty = 0, negPenalty = 0;

        foreach (var node in context.Flow.Nodes.Values)
        {
            if (node.Resolved.Fallacies.Count == 0)
                continue;

            var penalty = node.Resolved.Fallacies
                .Sum(f => context.Scoring.GetFallacyPenalty(f.ToString()));

            if (penalty <= 0) continue;

            // Penalty is negative — reduces the introducing side's score
            if (node.Side == Side.AFF) affPenalty += penalty;
            else                       negPenalty += penalty;

            var fallacyList = string.Join(", ", node.Resolved.Fallacies);
            details.Add(new ArgumentScoreDetail
            {
                ArgumentId       = node.ArgumentId,
                SpeechId         = node.SpeechId,
                StockIssueTag    = node.StockIssueTag,
                Side             = node.Side.ToString(),
                SpeakerId        = node.SpeakerId,
                RuleId           = RuleId,
                Score            = -penalty,
                ComputedStrength = node.ComputedStrength,
                IsDropped        = node.Status is ArgumentStatus.Dropped or ArgumentStatus.Conceded,
                FallacyPenalty   = penalty,
                Note             = $"Fallacies detected: [{fallacyList}] → -{penalty:F2} penalty"
            });
        }

        return new RuleResult
        {
            RuleId          = RuleId,
            DisplayName     = DisplayName,
            AffScore        = -affPenalty,   // negative contribution
            NegScore        = -negPenalty,
            ArgumentDetails = details,
            Explanation     = BuildExplanation(affPenalty, negPenalty, details)
        };
    }

    private static string BuildExplanation(double affPenalty, double negPenalty,
        List<ArgumentScoreDetail> details)
    {
        if (details.Count == 0)
            return "Logical Consistency: No fallacies detected. Both sides maintained sound reasoning.";

        return $"Logical Consistency: {details.Count} argument(s) contained logical fallacies. " +
               $"AFF penalty: -{affPenalty:F2}; NEG penalty: -{negPenalty:F2}.";
    }
}
