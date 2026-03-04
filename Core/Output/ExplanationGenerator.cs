using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.Scoring;
using FlowGraphNS = DebateScoringEngine.Core.FlowGraph;

namespace DebateScoringEngine.Core.Output;

/// <summary>
/// Generates human-readable explanations from a ScoringResult.
///
/// Separated from ScoringEngine so the explanation format can change
/// without touching scoring logic. Two output modes:
///
///   Full   — complete breakdown: winner, issue table, rule details,
///             flow graph summary, dropped arguments, notable moments.
///   Brief  — 3-5 sentence summary suitable for a ballot or API response.
///
/// All formatting is driven by the ScoringResult data — no re-scoring occurs here.
/// </summary>
public static class ExplanationGenerator
{
    // ── Public entry points ───────────────────────────────────────────────────

    public static string GenerateFull(
        ScoringResult result,
        FlowGraphNS.FlowGraph flow,
        FormatConfig format,
        RoundConfig round)
    {
        var sb = new System.Text.StringBuilder();

        WriteHeader(sb, round.Motion);
        WriteWinner(sb, result);
        WriteHardGateNotice(sb, result);
        WriteIssueTable(sb, result);
        WriteRuleBreakdown(sb, result);
        WriteFlowSummary(sb, flow);
        WriteNotableMoments(sb, result, flow);
        WriteFooter(sb);

        return sb.ToString();
    }

    public static string GenerateBrief(ScoringResult result, RoundConfig round)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Motion: {round.Motion}");
        sb.AppendLine();

        if (result.DecidedByHardGate)
        {
            sb.AppendLine($"Winner: {result.Winner} — decided on {result.HardGateIssue} (hard gate issue).");
            sb.AppendLine(result.WinnerExplanation);
        }
        else
        {
            sb.AppendLine($"Winner: {result.Winner} " +
                          $"(AFF {result.AffTotalScore:F2} — NEG {result.NegTotalScore:F2}).");
            sb.AppendLine(result.WinnerExplanation);

            var topIssue = result.StockIssueSummaries
                .Where(s => !s.IsHardGate)
                .OrderByDescending(s => Math.Abs(s.AffWeighted - s.NegWeighted))
                .FirstOrDefault();

            if (topIssue != null)
                sb.AppendLine($"The round turned primarily on {topIssue.IssueLabel} " +
                              $"(AFF {topIssue.AffWeighted:F2} vs NEG {topIssue.NegWeighted:F2}).");
        }

        var dropped = result.ArgumentDetails
            .Where(d => d.Note.Contains("dropped") || d.Note.Contains("conceded"))
            .Select(d => d.Side)
            .GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        if (dropped.Any())
        {
            var parts = dropped.Select(kv => $"{kv.Value} argument(s) dropped by {kv.Key}");
            sb.AppendLine($"Notable: {string.Join("; ", parts)}.");
        }

        return sb.ToString();
    }

    // ── Section writers ───────────────────────────────────────────────────────

    private static void WriteHeader(System.Text.StringBuilder sb, string motion)
    {
        sb.AppendLine(new string('═', 57));
        sb.AppendLine("  DEBATE SCORING RESULT");
        sb.AppendLine(new string('═', 57));
        sb.AppendLine();
        sb.AppendLine($"  Motion: {WrapText(motion, 53, "          ")}");
        sb.AppendLine();
    }

    private static void WriteWinner(System.Text.StringBuilder sb, ScoringResult result)
    {
        sb.AppendLine($"  ★  WINNER: {result.Winner}");
        sb.AppendLine();
        sb.AppendLine($"  {result.WinnerExplanation}");
        sb.AppendLine();
        if (!result.DecidedByHardGate)
        {
            sb.AppendLine($"  Final scores — AFF: {result.AffTotalScore:F3}  |  " +
                          $"NEG: {result.NegTotalScore:F3}");
            sb.AppendLine($"  Margin: {Math.Abs(result.AffTotalScore - result.NegTotalScore):F3}");
            sb.AppendLine();
        }
    }

    private static void WriteHardGateNotice(System.Text.StringBuilder sb, ScoringResult result)
    {
        if (!result.DecidedByHardGate) return;
        sb.AppendLine($"  ⚑  ROUND DECIDED BY HARD GATE: {result.HardGateIssue}");
        sb.AppendLine($"     The obligated side failed to win this issue.");
        sb.AppendLine($"     Score breakdown below is informational only.");
        sb.AppendLine();
    }

    private static void WriteIssueTable(System.Text.StringBuilder sb, ScoringResult result)
    {
        sb.AppendLine(new string('─', 57));
        sb.AppendLine("  STOCK ISSUE SUMMARY");
        sb.AppendLine(new string('─', 57));
        sb.AppendLine($"  {"Issue",-16} {"Weight",6}  {"AFF",8}  {"NEG",8}  {"Winner",8}");
        sb.AppendLine($"  {new string('─', 16),-16} {"──────",6}  {"────────",8}  {"────────",8}  {"────────",8}");

        foreach (var s in result.StockIssueSummaries)
        {
            var winnerCell = s.IsHardGate ? "(gate)" : (s.IssueWinner ?? "─");
            var weightCell = s.IsHardGate ? " GATE " : $"{s.Weight,6:P0}";
            var affCell    = s.IsHardGate ? $"{s.AffRawScore,8:F2}" : $"{s.AffWeighted,8:F2}";
            var negCell    = s.IsHardGate ? $"{s.NegRawScore,8:F2}" : $"{s.NegWeighted,8:F2}";
            sb.AppendLine($"  {s.IssueLabel,-16} {weightCell,6}  {affCell}  {negCell}  {winnerCell,8}");
        }

        sb.AppendLine();
    }

    private static void WriteRuleBreakdown(System.Text.StringBuilder sb, ScoringResult result)
    {
        sb.AppendLine(new string('─', 57));
        sb.AppendLine("  RULE-BY-RULE BREAKDOWN");
        sb.AppendLine(new string('─', 57));

        foreach (var r in result.RuleResults)
        {
            var affSign = r.AffScore >= 0 ? "+" : "";
            var negSign = r.NegScore >= 0 ? "+" : "";
            sb.AppendLine($"  [{r.DisplayName}]");
            sb.AppendLine($"    AFF: {affSign}{r.AffScore:F3}   NEG: {negSign}{r.NegScore:F3}");

            if (!string.IsNullOrEmpty(r.Explanation))
                sb.AppendLine($"    {r.Explanation}");

            // Show argument-level details if any penalties or notable scores
            var notable = r.ArgumentDetails
                .Where(d => Math.Abs(d.Score) > 0.5)
                .OrderByDescending(d => Math.Abs(d.Score))
                .Take(3);

            foreach (var d in notable)
                sb.AppendLine($"      • [{d.Side} {d.SpeechId}] {d.Note}");

            sb.AppendLine();
        }
    }

    private static void WriteFlowSummary(
        System.Text.StringBuilder sb,
        FlowGraphNS.FlowGraph flow)
    {
        var summary = flow.GetSummary();
        sb.AppendLine(new string('─', 57));
        sb.AppendLine("  FLOW GRAPH SUMMARY");
        sb.AppendLine(new string('─', 57));
        sb.AppendLine($"  Total arguments : {summary.TotalArguments}  " +
                      $"(AFF: {summary.AffArguments}, NEG: {summary.NegArguments})");
        sb.AppendLine($"  Total rebuttals : {summary.TotalEdges}");
        sb.AppendLine($"  Dropped args    : {summary.DroppedArguments}");
        sb.AppendLine();

        if (summary.ArgumentsByIssue.Any())
        {
            sb.AppendLine("  Arguments by issue:");
            foreach (var (issue, count) in summary.ArgumentsByIssue)
                sb.AppendLine($"    {issue,-18}: {count}");
            sb.AppendLine();
        }
    }

    private static void WriteNotableMoments(
        System.Text.StringBuilder sb,
        ScoringResult result,
        FlowGraphNS.FlowGraph flow)
    {
        var moments = new List<string>();

        // Dropped arguments
        var dropped = flow.GetDroppedNodes().ToList();
        foreach (var d in dropped)
            moments.Add($"[DROP] {d.Side} argument {d.ArgumentId} ({d.StockIssueTag}) " +
                        $"went unanswered — conceded to {d.Side}.");

        // CX admissions
        var admissions = result.ArgumentDetails
            .Where(d => d.ArgumentId.Contains(":admission"))
            .ToList();
        foreach (var a in admissions)
            moments.Add($"[CX]   Admission extracted by {a.Side} in {a.SpeechId}.");

        // Fallacy penalties > 0.5
        var fallacies = result.ArgumentDetails
            .Where(d => d.RuleId == "logical-consistency" && d.Score < -0.5)
            .ToList();
        foreach (var f in fallacies)
            moments.Add($"[FALLACY] {f.Side} arg {f.ArgumentId} ({f.StockIssueTag}): {f.Note}");

        // Strongest argument overall
        var strongest = result.ArgumentDetails
            .Where(d => d.RuleId == "argument-strength")
            .OrderByDescending(d => d.Score)
            .FirstOrDefault();
        if (strongest != null)
            moments.Add($"[BEST]  Strongest argument: {strongest.Side} {strongest.ArgumentId} " +
                        $"({strongest.StockIssueTag}, score {strongest.Score:F2}).");

        if (!moments.Any()) return;

        sb.AppendLine(new string('─', 57));
        sb.AppendLine("  NOTABLE MOMENTS");
        sb.AppendLine(new string('─', 57));
        foreach (var m in moments)
            sb.AppendLine($"  {m}");
        sb.AppendLine();
    }

    private static void WriteFooter(System.Text.StringBuilder sb)
    {
        sb.AppendLine(new string('═', 57));
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static string WrapText(string text, int width, string indent)
    {
        if (text.Length <= width) return text;
        var words  = text.Split(' ');
        var sb     = new System.Text.StringBuilder();
        var line   = new System.Text.StringBuilder();
        var first  = true;
        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > width)
            {
                if (!first) sb.AppendLine();
                sb.Append(first ? string.Empty : indent);
                sb.Append(line.ToString().TrimEnd());
                line.Clear();
                first = false;
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0)
        {
            if (!first) { sb.AppendLine(); sb.Append(indent); }
            sb.Append(line.ToString().TrimEnd());
        }
        return sb.ToString();
    }
}
