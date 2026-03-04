using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.Domain.Models;
using FlowGraphNS = DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Core.Scoring.Rules;

namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Orchestrates the deterministic scoring pipeline.
///
/// Sequence:
///   1. Build scoring context from all three configs + flow graph
///   2. Check hard gate issues — if any gate triggers, winner is decided immediately
///      (score breakdown still runs for informational output)
///   3. Run all rules — each produces independent AFF/NEG scores
///   4. Aggregate by stock issue — weighted sum per config
///   5. Determine winner — weighted total, tiebreaker by config priority list
///   6. Build ScoringResult with full breakdown and explanation
///
/// Extensibility: register additional IScoringRule implementations in BuildRules().
/// No other code changes required.
/// </summary>
public class ScoringEngine
{
    private readonly List<IScoringRule> _rules;

    public ScoringEngine()
    {
        _rules = BuildRules();
    }

    /// <summary>
    /// Constructor for testing — inject specific rules only.
    /// </summary>
    public ScoringEngine(IEnumerable<IScoringRule> rules)
    {
        _rules = rules.ToList();
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public ScoringResult Score(
        FlowGraphNS.FlowGraph flow,
        Debate debate,
        FormatConfig format,
        ScoringConfig scoring,
        RoundConfig round)
    {
        var context = new ScoringContext
        {
            Flow    = flow,
            Debate  = debate,
            Format  = format,
            Scoring = scoring,
            Round   = round
        };

        // Step 1: Run all rules (always — even if hard gate triggers, we show the breakdown)
        var ruleResults = RunAllRules(context);

        // Step 2: Build per-argument detail (flattened across all rules)
        var allArgumentDetails = ruleResults
            .SelectMany(r => r.ArgumentDetails)
            .ToList();

        // Step 2b: Populate cross-rule breakdown on every ArgumentScoreDetail
        // For each argument that appears in ANY rule, build a RuleBreakdown showing
        // all rules' contributions to that argument. Also aggregate penalty fields.
        PopulateCrossRuleBreakdown(ruleResults);

        // Step 2c: Build per-speaker summaries
        var speakerSummaries = BuildSpeakerSummaries(ruleResults, debate, flow);

        // Step 3: Build per-stock-issue summaries
        var issueSummaries = BuildIssueSummaries(ruleResults, format, scoring);

        // Step 4: Check hard gates
        var (hardGateTriggered, hardGateIssue, hardGateWinner) =
            CheckHardGates(context, issueSummaries);

        // Step 5: Determine final winner
        string winner;
        string winnerExplanation;

        if (hardGateTriggered)
        {
            winner            = hardGateWinner!;
            winnerExplanation = $"Round decided on {hardGateIssue}. " +
                                $"{hardGateWinner} wins because the obligated side lost the hard gate issue.";
        }
        else
        {
            var (weightedAff, weightedNeg) = ComputeWeightedTotals(issueSummaries);
            winner = DetermineWinner(weightedAff, weightedNeg, issueSummaries, scoring);
            winnerExplanation = BuildWinnerExplanation(winner, weightedAff, weightedNeg, issueSummaries);
        }

        // Step 6: Compute totals for display
        var affTotal = issueSummaries.Sum(s => s.AffWeighted);
        var negTotal = issueSummaries.Sum(s => s.NegWeighted);

        // Step 7: Build explanation
        var explanation = BuildFullExplanation(
            winner, winnerExplanation, hardGateTriggered, hardGateIssue,
            issueSummaries, ruleResults, flow);

        return new ScoringResult
        {
            Winner              = winner,
            WinnerExplanation   = winnerExplanation,
            DecidedByHardGate   = hardGateTriggered,
            HardGateIssue       = hardGateIssue,
            AffTotalScore       = affTotal,
            NegTotalScore       = negTotal,
            RuleResults         = ruleResults,
            StockIssueSummaries = issueSummaries,
            ArgumentDetails     = allArgumentDetails,
            SpeakerScoreSummaries = speakerSummaries,
            Explanation         = explanation
        };
    }

    // ── Step 1: Run rules ─────────────────────────────────────────────────────

    private List<RuleResult> RunAllRules(ScoringContext context) =>
        _rules.Select(rule => rule.Evaluate(context)).ToList();

    // ── Step 2: Build issue summaries ─────────────────────────────────────────

    private static List<StockIssueSummary> BuildIssueSummaries(
        List<RuleResult> ruleResults,
        FormatConfig format,
        ScoringConfig scoring)
    {
        var summaries = new List<StockIssueSummary>();

        foreach (var issue in format.StockIssues)
        {
            var isHardGate = format.HardGateIssues.Contains(issue.Id);
            var weight     = scoring.GetStockIssueWeight(issue.Id);

            // Sum AFF and NEG scores from all rules for arguments tagged to this issue
            var issueDetails = ruleResults
                .SelectMany(r => r.ArgumentDetails)
                .Where(d => d.StockIssueTag == issue.Id)
                .ToList();

            var affRaw = issueDetails.Where(d => d.Side == "AFF").Sum(d => d.Score);
            var negRaw = issueDetails.Where(d => d.Side == "NEG").Sum(d => d.Score);

            // Weighted totals (hard gate issues have weight 0 in scoring config)
            var affWeighted = affRaw * weight;
            var negWeighted = negRaw * weight;

            string? issueWinner = null;
            if (!isHardGate)
                issueWinner = affWeighted >= negWeighted ? "AFF" : "NEG";

            summaries.Add(new StockIssueSummary
            {
                IssueId     = issue.Id,
                IssueLabel  = issue.Label,
                AffRawScore = affRaw,
                NegRawScore = negRaw,
                AffWeighted = affWeighted,
                NegWeighted = negWeighted,
                Weight      = weight,
                IssueWinner = issueWinner,
                IsHardGate  = isHardGate,
                Notes       = isHardGate ? "Hard gate issue — decided separately" : string.Empty
            });
        }

        // Also include CX and other non-stock-issue categories
        var specialTags = new[] { "CrossEx", "TimeEfficiency", "PrepTime" };
        foreach (var tag in specialTags)
        {
            var tagDetails = ruleResults
                .SelectMany(r => r.ArgumentDetails)
                .Where(d => d.StockIssueTag == tag)
                .ToList();
            if (!tagDetails.Any()) continue;

            var weight     = scoring.GetStockIssueWeight(tag);
            var affRaw     = tagDetails.Where(d => d.Side == "AFF").Sum(d => d.Score);
            var negRaw     = tagDetails.Where(d => d.Side == "NEG").Sum(d => d.Score);

            summaries.Add(new StockIssueSummary
            {
                IssueId     = tag,
                IssueLabel  = tag,
                AffRawScore = affRaw,
                NegRawScore = negRaw,
                AffWeighted = affRaw * weight,
                NegWeighted = negRaw * weight,
                Weight      = weight,
                IssueWinner = affRaw * weight >= negRaw * weight ? "AFF" : "NEG",
                IsHardGate  = false,
            });
        }

        return summaries;
    }

    // ── Step 3: Hard gate check ───────────────────────────────────────────────

    /// <summary>
    /// Checks each hard gate issue. The OBLIGATED side must win the issue;
    /// if they lose it, the other side wins the round immediately.
    ///
    /// For AFF-obligated issues (e.g. Topicality): AFF must have positive net score.
    /// If AFF's net score is zero or negative on a hard gate issue, NEG wins.
    ///
    /// Returns (triggered, issueName, winner) — winner is null if no gate triggered.
    /// </summary>
    private static (bool triggered, string? issue, string? winner) CheckHardGates(
        ScoringContext context,
        List<StockIssueSummary> summaries)
    {
        foreach (var gateIssueId in context.Format.HardGateIssues)
        {
            var summary = summaries.FirstOrDefault(s => s.IssueId == gateIssueId);
            if (summary == null) continue;

            var issueDef = context.Format.StockIssues
                .FirstOrDefault(i => i.Id == gateIssueId);
            if (issueDef == null) continue;

            var obligatedSide = issueDef.ObligatedSide; // "AFF" or "NEG"

            // The obligated side must have a strictly positive net raw score
            var obligatedScore = obligatedSide == "AFF"
                ? summary.AffRawScore
                : summary.NegRawScore;

            if (obligatedScore <= 0)
            {
                // Obligated side failed — other side wins
                var winner = obligatedSide == "AFF" ? "NEG" : "AFF";
                return (true, gateIssueId, winner);
            }
        }

        return (false, null, null);
    }

    // ── Step 4: Weighted totals + winner ──────────────────────────────────────

    private static (double aff, double neg) ComputeWeightedTotals(
        List<StockIssueSummary> summaries)
    {
        var aff = summaries.Where(s => !s.IsHardGate).Sum(s => s.AffWeighted);
        var neg = summaries.Where(s => !s.IsHardGate).Sum(s => s.NegWeighted);
        return (aff, neg);
    }

    private static string DetermineWinner(
        double weightedAff,
        double weightedNeg,
        List<StockIssueSummary> summaries,
        ScoringConfig scoring)
    {
        if (Math.Abs(weightedAff - weightedNeg) > 1e-9)
            return weightedAff > weightedNeg ? "AFF" : "NEG";

        // Tiebreaker: iterate priority list, first issue where one side leads wins
        foreach (var tiebreakerId in scoring.TiebreakerPriority)
        {
            var s = summaries.FirstOrDefault(x => x.IssueId == tiebreakerId);
            if (s == null) continue;
            if (Math.Abs(s.AffWeighted - s.NegWeighted) > 1e-9)
                return s.AffWeighted > s.NegWeighted ? "AFF" : "NEG";
        }

        // If all tiebreakers also tie — AFF wins (benefit of the doubt to the affirmative)
        return "AFF";
    }

    // ── Explanation builders ──────────────────────────────────────────────────

    private static string BuildWinnerExplanation(
        string winner,
        double weightedAff,
        double weightedNeg,
        List<StockIssueSummary> summaries)
    {
        var issuesWon = summaries
            .Where(s => !s.IsHardGate && s.IssueWinner == winner)
            .Select(s => s.IssueLabel)
            .ToList();

        return $"{winner} wins on weighted score ({weightedAff:F2} vs {weightedNeg:F2}). " +
               (issuesWon.Any()
                   ? $"{winner} won the following issues: {string.Join(", ", issuesWon)}."
                   : string.Empty);
    }

    private static string BuildFullExplanation(
        string winner,
        string winnerExplanation,
        bool hardGate,
        string? hardGateIssue,
        List<StockIssueSummary> summaries,
        List<RuleResult> ruleResults,
        FlowGraphNS.FlowGraph flow)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine("  DEBATE SCORING RESULT");
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  WINNER: {winner}");
        sb.AppendLine($"  {winnerExplanation}");
        sb.AppendLine();

        if (hardGate)
        {
            sb.AppendLine($"  ⚑  Round decided by hard gate on: {hardGateIssue}");
            sb.AppendLine($"     Score breakdown below is informational only.");
            sb.AppendLine();
        }

        // Stock issue summary table
        sb.AppendLine("─── Stock Issue Summary ────────────────────────────────");
        sb.AppendLine($"  {"Issue",-18} {"Weight",6}  {"AFF",8}  {"NEG",8}  {"Winner",6}");
        sb.AppendLine($"  {"─────",-18} {"──────",6}  {"───",8}  {"───",8}  {"──────",6}");
        foreach (var s in summaries.Where(s => !s.IsHardGate))
        {
            sb.AppendLine(
                $"  {s.IssueLabel,-18} {s.Weight,6:P0}  {s.AffWeighted,8:F2}  {s.NegWeighted,8:F2}  {s.IssueWinner ?? "─",6}");
        }
        if (hardGate && hardGateIssue != null)
        {
            var hs = summaries.FirstOrDefault(s => s.IssueId == hardGateIssue);
            if (hs != null)
                sb.AppendLine(
                    $"  {hs.IssueLabel,-18} {"GATE",6}  {hs.AffRawScore,8:F2}  {hs.NegRawScore,8:F2}  {"→ GATE",6}");
        }
        sb.AppendLine();

        // Rule-by-rule breakdown
        sb.AppendLine("─── Rule Breakdown ─────────────────────────────────────");
        foreach (var r in ruleResults)
        {
            sb.AppendLine($"  [{r.DisplayName}]");
            sb.AppendLine($"    AFF: {r.AffScore:+0.00;-0.00;0.00}  NEG: {r.NegScore:+0.00;-0.00;0.00}");
            sb.AppendLine($"    {r.Explanation}");
        }
        sb.AppendLine();

        // Flow graph notes
        var graphSummary = flow.GetSummary();
        sb.AppendLine("─── Flow Graph Summary ─────────────────────────────────");
        sb.AppendLine($"  Total arguments : {graphSummary.TotalArguments}  " +
                      $"(AFF: {graphSummary.AffArguments}, NEG: {graphSummary.NegArguments})");
        sb.AppendLine($"  Total rebuttals : {graphSummary.TotalEdges}");
        sb.AppendLine($"  Dropped args    : {graphSummary.DroppedArguments}");
        sb.AppendLine();

        var dropped = flow.GetDroppedNodes().ToList();
        if (dropped.Any())
        {
            sb.AppendLine("  Dropped arguments (opponent failed to respond):");
            foreach (var d in dropped)
                sb.AppendLine($"    • [{d.Side}] {d.ArgumentId} ({d.SpeechId}, {d.StockIssueTag})");
        }

        sb.AppendLine("═══════════════════════════════════════════════════════");
        return sb.ToString();
    }

    // ── Rule registration ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the default ordered list of scoring rules.
    /// To add a new rule: implement IScoringRule, instantiate it here.
    /// </summary>
    private static List<IScoringRule> BuildRules() => new()
    {
        new ArgumentStrengthRule(),
        new DroppedArgumentRule(),
        new LogicalConsistencyRule(),
        new RebuttalEffectivenessRule(),
        new TimeEfficiencyRule(),
        new CrossExaminationRule(),
        new PrepTimeRule(),
    };

    // ── Cross-rule aggregation ────────────────────────────────────────────────

    /// <summary>
    /// Builds per-speaker score summaries by aggregating all ArgumentScoreDetails
    /// that have a non-null SpeakerId. Entries without a SpeakerId (e.g. prep time)
    /// are excluded from individual speaker scores.
    /// </summary>
    private static List<SpeakerScoreSummary> BuildSpeakerSummaries(
        List<RuleResult> ruleResults,
        Debate debate,
        FlowGraphNS.FlowGraph flow)
    {
        // Build speaker lookup
        var speakerMap = debate.Speakers.ToDictionary(s => s.SpeakerId);

        // Collect all details that have a speaker
        var allDetails = ruleResults
            .SelectMany(r => r.ArgumentDetails)
            .Where(d => d.SpeakerId != null)
            .ToList();

        // Group by speaker
        var grouped = allDetails.GroupBy(d => d.SpeakerId!);

        var summaries = new List<SpeakerScoreSummary>();

        foreach (var group in grouped)
        {
            var speakerId = group.Key;
            var speaker = speakerMap.GetValueOrDefault(speakerId);
            var details = group.ToList();

            // Count unique arguments (not counting the same arg across multiple rules)
            var uniqueArgIds = details
                .Select(d => d.ArgumentId)
                .Where(id => !id.StartsWith("cx:") && !id.StartsWith("speech:") && !id.StartsWith("prep:"))
                .Distinct()
                .ToList();

            // Count dropped arguments
            var droppedCount = details
                .Where(d => d.IsDropped && d.RuleId == "dropped-argument")
                .Select(d => d.ArgumentId)
                .Distinct()
                .Count();

            // Count rebuttals
            var rebuttalCount = details
                .Where(d => d.RuleId == "rebuttal-effectiveness")
                .Select(d => d.ArgumentId)
                .Distinct()
                .Count();

            // Average strength from argument-strength rule entries
            var strengthEntries = details
                .Where(d => d.RuleId == "argument-strength")
                .ToList();
            var avgStrength = strengthEntries.Count > 0
                ? strengthEntries.Average(d => d.ComputedStrength)
                : 0;

            // Per-rule contributions
            var ruleContributions = ruleResults
                .Select(rule =>
                {
                    var speakerDetails = rule.ArgumentDetails
                        .Where(d => d.SpeakerId == speakerId)
                        .ToList();
                    return new SpeakerRuleContribution
                    {
                        RuleId      = rule.RuleId,
                        DisplayName = rule.DisplayName,
                        Score       = speakerDetails.Sum(d => d.Score),
                        DetailCount = speakerDetails.Count,
                    };
                })
                .Where(c => c.DetailCount > 0) // Only include rules where this speaker has entries
                .ToList();

            summaries.Add(new SpeakerScoreSummary
            {
                SpeakerId        = speakerId,
                SpeakerName      = speaker?.Name ?? speakerId,
                Side             = speaker?.Side.ToString() ?? details.First().Side,
                TotalScore       = details.Sum(d => d.Score),
                ArgumentCount    = uniqueArgIds.Count,
                DroppedCount     = droppedCount,
                RebuttalCount    = rebuttalCount,
                AverageStrength  = avgStrength,
                RuleContributions = ruleContributions,
            });
        }

        // Sort: AFF speakers first, then by total score descending
        return summaries
            .OrderBy(s => s.Side == "AFF" ? 0 : 1)
            .ThenByDescending(s => s.TotalScore)
            .ToList();
    }

    /// <summary>
    /// After all rules have run, populates each ArgumentScoreDetail with a
    /// RuleBreakdown showing every rule's contribution to that argument.
    /// Also aggregates DroppedPenalty and FallacyPenalty from the relevant rules.
    /// </summary>
    private static void PopulateCrossRuleBreakdown(List<RuleResult> ruleResults)
    {
        // Build a lookup: argumentId → list of (ruleDisplayName, score, note)
        var crossRuleMap = new Dictionary<string, List<RuleScore>>();
        var droppedPenalties = new Dictionary<string, double>();
        var fallacyPenalties = new Dictionary<string, double>();

        foreach (var rule in ruleResults)
        {
            foreach (var detail in rule.ArgumentDetails)
            {
                if (!crossRuleMap.ContainsKey(detail.ArgumentId))
                    crossRuleMap[detail.ArgumentId] = new List<RuleScore>();

                crossRuleMap[detail.ArgumentId].Add(new RuleScore
                {
                    RuleName = rule.DisplayName,
                    Score    = detail.Score,
                    Notes    = detail.Note,
                });

                // Aggregate penalties by type
                if (rule.RuleId == "dropped-argument" && detail.DroppedPenalty > 0)
                    droppedPenalties[detail.ArgumentId] = detail.DroppedPenalty;

                if (rule.RuleId == "logical-consistency" && detail.FallacyPenalty > 0)
                    fallacyPenalties[detail.ArgumentId] = detail.FallacyPenalty;
            }
        }

        // Now inject the cross-rule breakdown into every detail
        foreach (var rule in ruleResults)
        {
            foreach (var detail in rule.ArgumentDetails)
            {
                if (crossRuleMap.TryGetValue(detail.ArgumentId, out var breakdown))
                    detail.RuleBreakdown = breakdown;

                if (droppedPenalties.TryGetValue(detail.ArgumentId, out var dp))
                    detail.DroppedPenalty = dp;

                if (fallacyPenalties.TryGetValue(detail.ArgumentId, out var fp))
                    detail.FallacyPenalty = fp;
            }
        }
    }
}
