using DebateScoringEngine.Core.Config;
using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.Domain.Models;

namespace DebateScoringEngine.Core.FlowGraph;

/// <summary>
/// Constructs a fully resolved FlowGraph from a Debate and its three configs.
///
/// Build sequence:
///   1. Create one ArgumentNode per argument
///   2. Create RebuttalEdges from RebuttalTargetIds
///   3. Resolve enrichment for each node (explicit → blueprint → default)
///   4. Compute argument strength for each node
///   5. Run drop detection
///   6. Apply explicit status overrides (human/LLM enrichment)
/// </summary>
public class FlowGraphBuilder
{
    private readonly FormatConfig  _format;
    private readonly ScoringConfig _scoring;
    private readonly RoundConfig   _round;

    public FlowGraphBuilder(FormatConfig format, ScoringConfig scoring, RoundConfig round)
    {
        _format  = format;
        _scoring = scoring;
        _round   = round;
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public FlowGraph Build(Debate debate)
    {
        var nodes = BuildNodes(debate);
        var edges = BuildEdges(debate, nodes);

        ResolveAllEnrichment(nodes);
        ComputeAllStrengths(nodes);
        DetectDrops(nodes, edges);
        ApplyStatusOverrides(nodes);

        return new FlowGraph(debate.DebateId, nodes, edges);
    }

    // ── Step 1: Build nodes ───────────────────────────────────────────────────

    private static Dictionary<string, ArgumentNode> BuildNodes(Debate debate)
    {
        var nodes = new Dictionary<string, ArgumentNode>(debate.Arguments.Count);
        foreach (var (id, argument) in debate.Arguments)
            nodes[id] = new ArgumentNode(argument);
        return nodes;
    }

    // ── Step 2: Build edges ───────────────────────────────────────────────────

    private static List<RebuttalEdge> BuildEdges(
        Debate debate,
        Dictionary<string, ArgumentNode> nodes)
    {
        var edges = new List<RebuttalEdge>();

        foreach (var (_, argument) in debate.Arguments)
        {
            foreach (var targetId in argument.RebuttalTargetIds)
            {
                // Skip dangling references — target argument must exist in the debate
                if (!nodes.TryGetValue(targetId, out var targetNode))
                    continue;

                edges.Add(new RebuttalEdge(
                    sourceArgumentId: argument.ArgumentId,
                    sourceSpeechId:   argument.SpeechId,
                    targetArgumentId: targetId,
                    targetSpeechId:   targetNode.SpeechId));
            }
        }
        return edges;
    }

    // ── Step 3: Resolve enrichment ────────────────────────────────────────────

    /// <summary>
    /// For every node, resolves each enrichment field via the three-tier fallback:
    ///   Tier 1 — Explicit enrichment on the argument (set by human/LLM)
    ///   Tier 2 — Stock case blueprint defaults (if argument has a StockCaseId)
    ///   Tier 3 — Global scoring config defaults (always present)
    ///
    /// Stores the resolved values in node.Resolved along with provenance strings
    /// so the explanation generator can report what data source was used.
    /// </summary>
    private void ResolveAllEnrichment(Dictionary<string, ArgumentNode> nodes)
    {
        foreach (var node in nodes.Values)
            node.Resolved = ResolveEnrichment(node.Argument);
    }

    private ResolvedEnrichment ResolveEnrichment(Argument argument)
    {
        var enrichment = argument.Enrichment;
        var blueprint  = argument.StockCaseId != null
            ? _round.FindBlueprint(argument.StockCaseId)
            : null;

        var resolved = new ResolvedEnrichment();

        // ── EvidenceQuality ──────────────────────────────────────────────────
        if (enrichment.EvidenceQuality.HasValue)
        {
            resolved.EvidenceQuality = enrichment.EvidenceQuality.Value;
            resolved.EvidenceSource  = "explicit";
        }
        else if (blueprint?.DefaultEnrichment.EvidenceQuality != null &&
                 Enum.TryParse<EvidenceQuality>(
                     blueprint.DefaultEnrichment.EvidenceQuality, true, out var bpEq))
        {
            resolved.EvidenceQuality = bpEq;
            resolved.EvidenceSource  = $"blueprint:{argument.StockCaseId}";
        }
        else if (Enum.TryParse<EvidenceQuality>(
                     _scoring.DefaultEvidenceQuality, true, out var defEq))
        {
            resolved.EvidenceQuality = defEq;
            resolved.EvidenceSource  = "default";
        }

        // ── ImpactMagnitude ──────────────────────────────────────────────────
        if (enrichment.ImpactMagnitude.HasValue)
        {
            resolved.ImpactMagnitude = enrichment.ImpactMagnitude.Value;
            resolved.ImpactSource    = "explicit";
        }
        else if (blueprint?.DefaultEnrichment.ImpactMagnitude != null &&
                 Enum.TryParse<ImpactMagnitude>(
                     blueprint.DefaultEnrichment.ImpactMagnitude, true, out var bpIm))
        {
            resolved.ImpactMagnitude = bpIm;
            resolved.ImpactSource    = $"blueprint:{argument.StockCaseId}";
        }
        else if (Enum.TryParse<ImpactMagnitude>(
                     _scoring.DefaultImpactMagnitude, true, out var defIm))
        {
            resolved.ImpactMagnitude = defIm;
            resolved.ImpactSource    = "default";
        }

        // ── Fallacies ────────────────────────────────────────────────────────
        // Explicit wins; if none, try blueprint; if none, empty list
        if (enrichment.Fallacies != null && enrichment.Fallacies.Count > 0)
        {
            resolved.Fallacies = new List<FallacyType>(enrichment.Fallacies);
        }
        else if (blueprint?.DefaultEnrichment.Fallacies != null)
        {
            resolved.Fallacies = blueprint.DefaultEnrichment.Fallacies
                .Select(f => Enum.TryParse<FallacyType>(f, true, out var ft) ? ft : (FallacyType?)null)
                .Where(ft => ft.HasValue)
                .Select(ft => ft!.Value)
                .ToList();
        }

        // ── ArgumentStrength (explicit override) ─────────────────────────────
        if (enrichment.ArgumentStrength.HasValue)
        {
            resolved.StrengthExplicit  = true;
            resolved.ExplicitStrength  = enrichment.ArgumentStrength.Value;
        }

        return resolved;
    }

    // ── Step 4: Compute argument strength ─────────────────────────────────────

    /// <summary>
    /// Computes ComputedStrength for every node.
    ///
    /// If an explicit ArgumentStrength was provided, use it directly (clamped to 0–5).
    /// Otherwise: (ImpactScore × EvidenceMultiplier) − sum(FallacyPenalties), clamped 0–5.
    /// </summary>
    private void ComputeAllStrengths(Dictionary<string, ArgumentNode> nodes)
    {
        foreach (var node in nodes.Values)
            node.ComputedStrength = ComputeStrength(node);
    }

    private double ComputeStrength(ArgumentNode node)
    {
        var r = node.Resolved;

        if (r.StrengthExplicit && r.ExplicitStrength.HasValue)
            return Clamp(r.ExplicitStrength.Value, 0, 5);

        var impactScore      = _scoring.GetImpactScore(r.ImpactMagnitude.ToString());
        var evidenceMultiplier = _scoring.GetEvidenceMultiplier(r.EvidenceQuality.ToString());
        var fallacyPenalty   = r.Fallacies.Sum(f => _scoring.GetFallacyPenalty(f.ToString()));

        var strength = (impactScore * evidenceMultiplier) - fallacyPenalty;
        return Clamp(strength, 0, 5);
    }

    // ── Step 5: Drop detection ─────────────────────────────────────────────────

    /// <summary>
    /// Marks argument nodes as Dropped when an opponent failed to respond by the required speech.
    ///
    /// Algorithm:
    ///   For each argument:
    ///     1. Find the drop rule for the speech it was introduced in
    ///     2. Get the cutoff speech index from the ordered speech list
    ///     3. Check if any rebuttal edge targets this argument from a speech ≤ cutoff
    ///     4. If no response arrived in time AND no explicit status override: mark Dropped
    ///
    /// Dropped arguments award the introducing side a score bonus in DroppedArgumentRule.
    /// </summary>
    internal void DetectDrops(
        Dictionary<string, ArgumentNode> nodes,
        List<RebuttalEdge> edges)
    {
        foreach (var node in nodes.Values)
        {
            // If the argument already has an explicit status override, skip it —
            // the human or LLM has made a judgment call; we respect it.
            if (node.Argument.Enrichment.Status.HasValue)
                continue;

            var dropRule = _format.DropRules
                .FirstOrDefault(r => r.ArgumentIntroducedIn == node.SpeechId);

            if (dropRule == null)
                continue; // No drop obligation defined for this speech

            var cutoffIndex = _format.GetSpeechIndex(dropRule.MustBeAnsweredBy);
            if (cutoffIndex < 0)
                continue; // Cutoff speech not found in format — skip rather than error

            // Check if any rebuttal arrived at or before the cutoff speech
            var hasTimelResponse = edges.Any(e =>
                e.TargetArgumentId == node.ArgumentId &&
                _format.GetSpeechIndex(e.SourceSpeechId) <= cutoffIndex);

            if (!hasTimelResponse)
                node.Status = ArgumentStatus.Dropped;
        }
    }

    // ── Step 6: Apply explicit status overrides ───────────────────────────────

    /// <summary>
    /// Applies explicit status values from enrichment, overriding engine-derived status.
    /// This runs AFTER drop detection so human/LLM can restore an argument
    /// the engine would have marked Dropped (e.g., they noted a late response counts).
    /// Sets StatusIsOverridden = true so the explanation generator can report it.
    /// </summary>
    private static void ApplyStatusOverrides(Dictionary<string, ArgumentNode> nodes)
    {
        foreach (var node in nodes.Values)
        {
            if (node.Argument.Enrichment.Status.HasValue)
            {
                node.Status             = node.Argument.Enrichment.Status.Value;
                node.StatusIsOverridden = true;
            }
        }
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));
}
