using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Tests.Helpers;

namespace DebateScoringEngine.Tests;

public static class FlowGraphBuilderTests
{
    public static void Run()
    {
        TestRunner.Section("FlowGraphBuilder — Node and Edge Construction");

        var format  = DebateFactory.StandardFormat();
        var scoring = DebateFactory.StandardScoring();
        var round   = DebateFactory.EmptyRound();
        var builder = new FlowGraphBuilder(format, scoring, round);

        // Single argument produces one node
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(1, graph.Nodes.Count, "Single arg → one node");
            TestRunner.Assert(graph.GetNode("a1") != null, "Node retrievable by ID");
        }

        // Rebuttal creates an edge between nodes
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "a1" })
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(1, graph.Edges.Count, "One rebuttal → one edge");
            TestRunner.AssertEqual("n1", graph.Edges[0].SourceArgumentId, "Edge source is rebuttal");
            TestRunner.AssertEqual("a1", graph.Edges[0].TargetArgumentId, "Edge target is original");
        }

        // Dangling rebuttal reference (target doesn't exist) is silently skipped
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "ghost-id" })
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(0, graph.Edges.Count, "Dangling rebuttal target silently skipped");
        }

        // Multiple rebuttals from one argument
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),
                DebateFactory.Arg("a2", "1AC", Side.AFF, "Solvency"),
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "a1", "a2" })
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(2, graph.Edges.Count, "One arg rebutting two targets → two edges");
        }

        TestRunner.Section("FlowGraphBuilder — Blueprint Resolution");

        // Explicit enrichment takes priority over blueprint
        {
            var blueprint = new DebateScoringEngine.Core.Config.StockCase
            {
                StockCaseId   = "sc-test",
                Label         = "Test blueprint",
                StockIssueTag = "Harms",
                Side          = "AFF",
                DefaultEnrichment = new()
                {
                    EvidenceQuality = "PeerReviewed",
                    ImpactMagnitude = "Catastrophic",
                }
            };
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    stockCaseId: "sc-test",
                    evidence:    EvidenceQuality.Anecdotal,   // explicit overrides blueprint
                    impact:      ImpactMagnitude.Negligible)  // explicit overrides blueprint
            );
            var graph = builder.Build(DebateFactory.RoundWithBlueprint(blueprint), debate);
            var node = graph.GetNode("a1")!;
            TestRunner.AssertEqual(EvidenceQuality.Anecdotal,  node.Resolved.EvidenceQuality,
                "Explicit evidence beats blueprint");
            TestRunner.AssertEqual(ImpactMagnitude.Negligible, node.Resolved.ImpactMagnitude,
                "Explicit impact beats blueprint");
            TestRunner.AssertEqual("explicit", node.Resolved.EvidenceSource,
                "Evidence source is 'explicit'");
        }

        // Blueprint fills null enrichment fields
        {
            var blueprint = new DebateScoringEngine.Core.Config.StockCase
            {
                StockCaseId   = "sc-test2",
                Label         = "Test blueprint 2",
                StockIssueTag = "Harms",
                Side          = "AFF",
                DefaultEnrichment = new()
                {
                    EvidenceQuality = "PeerReviewed",
                    ImpactMagnitude = "Significant",
                }
            };
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms", stockCaseId: "sc-test2")
                // No explicit enrichment — all null
            );
            var graph = builder.Build(DebateFactory.RoundWithBlueprint(blueprint), debate);
            var node = graph.GetNode("a1")!;
            TestRunner.AssertEqual(EvidenceQuality.PeerReviewed, node.Resolved.EvidenceQuality,
                "Blueprint fills null evidence quality");
            TestRunner.AssertEqual(ImpactMagnitude.Significant,  node.Resolved.ImpactMagnitude,
                "Blueprint fills null impact magnitude");
            TestRunner.Assert(node.Resolved.EvidenceSource.StartsWith("blueprint:"),
                "Evidence source identifies blueprint");
        }

        // Global default is lowest fallback
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")
                // No stockCaseId, no explicit enrichment → all defaults
            );
            var graph = builder.Build(debate);
            var node = graph.GetNode("a1")!;
            TestRunner.AssertEqual(EvidenceQuality.Unverified, node.Resolved.EvidenceQuality,
                "Global default: Unverified evidence");
            TestRunner.AssertEqual(ImpactMagnitude.Minor, node.Resolved.ImpactMagnitude,
                "Global default: Minor impact");
            TestRunner.AssertEqual("default", node.Resolved.EvidenceSource,
                "Evidence source is 'default'");
        }

        TestRunner.Section("FlowGraphBuilder — Computed Strength");

        // Strength = ImpactScore * EvidenceMultiplier (no fallacies)
        // Minor (2.0) * Unverified (0.25) = 0.5
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF,
                    evidence: EvidenceQuality.Unverified,
                    impact:   ImpactMagnitude.Minor)
            );
            var graph = builder.Build(debate);
            var node = graph.GetNode("a1")!;
            TestRunner.AssertEqual(0.5, node.ComputedStrength, "Strength: Minor×Unverified=0.5");
        }

        // Significant (3.0) * PeerReviewed (1.0) = 3.0
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF,
                    evidence: EvidenceQuality.PeerReviewed,
                    impact:   ImpactMagnitude.Significant)
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(3.0, graph.GetNode("a1")!.ComputedStrength,
                "Strength: Significant×PeerReviewed=3.0");
        }

        // Fallacy reduces strength: 3.0 - StrawMan(0.5) = 2.5
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF,
                    evidence:  EvidenceQuality.PeerReviewed,
                    impact:    ImpactMagnitude.Significant,
                    fallacies: new() { FallacyType.StrawMan })
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(2.5, graph.GetNode("a1")!.ComputedStrength,
                "StrawMan fallacy reduces strength by 0.5");
        }

        // Explicit strength overrides formula
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF,
                    evidence: EvidenceQuality.Unverified,
                    impact:   ImpactMagnitude.Negligible,
                    strength: 4.8)  // explicit — should override formula
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(4.8, graph.GetNode("a1")!.ComputedStrength,
                "Explicit strength overrides formula");
        }

        // Strength clamped to 0 (can't go negative)
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF,
                    evidence:  EvidenceQuality.Unverified,
                    impact:    ImpactMagnitude.Negligible,     // 1.0 * 0.25 = 0.25
                    fallacies: new() { FallacyType.AdHominem }) // -0.75 → result = -0.5 → clamped to 0
            );
            var graph = builder.Build(debate);
            TestRunner.Assert(graph.GetNode("a1")!.ComputedStrength == 0.0,
                "Strength clamped to 0 when formula goes negative");
        }
    }

    // Helper overload that accepts round config explicitly
    private static FlowGraph Build(this FlowGraphBuilder builder,
        DebateScoringEngine.Core.Config.RoundConfig round,
        DebateScoringEngine.Core.Domain.Models.Debate debate)
    {
        var format  = DebateFactory.StandardFormat();
        var scoring = DebateFactory.StandardScoring();
        var b = new FlowGraphBuilder(format, scoring, round);
        return b.Build(debate);
    }
}
