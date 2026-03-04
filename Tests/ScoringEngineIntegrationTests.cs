using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Core.Scoring;
using DebateScoringEngine.Tests.Helpers;

namespace DebateScoringEngine.Tests;

public static class ScoringEngineIntegrationTests
{
    public static void Run()
    {
        TestRunner.Section("ScoringEngine — Hard Gate");

        // NEG wins Topicality hard gate — AFF has no T arguments
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1",  "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("n-t", "1NC", Side.NEG, "Topicality",
                    evidence: EvidenceQuality.ExpertOpinion, impact: ImpactMagnitude.Significant)
            );
            var result = Score(debate);
            TestRunner.AssertEqual("NEG", result.Winner,
                "HardGate: NEG wins when AFF has no Topicality arguments");
            TestRunner.Assert(result.DecidedByHardGate, "HardGate: DecidedByHardGate flag set");
            TestRunner.AssertEqual("Topicality", result.HardGateIssue, "HardGate: issue is Topicality");
        }

        // AFF answers Topicality — no hard gate trigger
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1",  "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("a-t", "1AC", Side.AFF, "Topicality",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("n-t", "1NC", Side.NEG, "Topicality",
                    rebuttalTargets: new[] { "a-t" },
                    evidence: EvidenceQuality.ExpertOpinion, impact: ImpactMagnitude.Minor),
                DebateFactory.Arg("n1",  "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.Anecdotal, impact: ImpactMagnitude.Minor)
            );
            var result = Score(debate);
            TestRunner.Assert(!result.DecidedByHardGate,
                "HardGate: No trigger when AFF wins Topicality");
        }

        TestRunner.Section("ScoringEngine — Weighted Winner");

        // AFF wins on weighted score — stronger harms + solvency
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a-t", "1AC", Side.AFF, "Topicality",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("a1",  "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Catastrophic),
                DebateFactory.Arg("a2",  "1AC", Side.AFF, "Solvency",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("n-t", "1NC", Side.NEG, "Topicality",
                    rebuttalTargets: new[] { "a-t" },
                    evidence: EvidenceQuality.Anecdotal, impact: ImpactMagnitude.Minor),
                DebateFactory.Arg("n1",  "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.Unverified, impact: ImpactMagnitude.Minor),
                DebateFactory.Arg("n2",  "1NC", Side.NEG, "Solvency",
                    rebuttalTargets: new[] { "a2" },
                    evidence: EvidenceQuality.Anecdotal, impact: ImpactMagnitude.Minor)
            );
            var result = Score(debate);
            TestRunner.AssertEqual("AFF", result.Winner,
                "Weighted: AFF wins with stronger arguments");
            TestRunner.Assert(result.AffTotalScore > result.NegTotalScore,
                "Weighted: AFF total score exceeds NEG");
        }

        TestRunner.Section("ScoringEngine — Score Breakdown Completeness");

        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a-t", "1AC", Side.AFF, "Topicality",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Minor),
                DebateFactory.Arg("a1",  "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("n1",  "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.ExpertOpinion, impact: ImpactMagnitude.Minor)
            );
            var result = Score(debate);
            TestRunner.Assert(result.RuleResults.Count >= 7, "Breakdown: All 7 rules ran");
            TestRunner.Assert(result.StockIssueSummaries.Count > 0, "Breakdown: Issue summaries present");
            TestRunner.Assert(result.ArgumentDetails.Count > 0, "Breakdown: Argument details present");
            TestRunner.Assert(result.Explanation.Contains("WINNER"), "Breakdown: Explanation has WINNER");
            TestRunner.Assert(result.Explanation.Contains("Stock Issue Summary"),
                "Breakdown: Explanation has issue table");
        }

        TestRunner.Section("ScoringEngine — Determinism");

        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a-t", "1AC", Side.AFF, "Topicality",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("a1",  "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Catastrophic),
                DebateFactory.Arg("n1",  "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.ExpertOpinion, impact: ImpactMagnitude.Significant)
            );
            var r1 = Score(debate);
            var r2 = Score(debate);
            var r3 = Score(debate);
            TestRunner.AssertEqual(r1.Winner, r2.Winner, "Determinism: Run 1 == Run 2 winner");
            TestRunner.AssertEqual(r1.Winner, r3.Winner, "Determinism: Run 1 == Run 3 winner");
            TestRunner.Assert(Math.Abs(r1.AffTotalScore - r2.AffTotalScore) < 1e-9,
                "Determinism: AFF scores identical across runs");
        }
    }

    private static ScoringResult Score(
        DebateScoringEngine.Core.Domain.Models.Debate debate)
    {
        var format  = DebateFactory.StandardFormat();
        var scoring = DebateFactory.StandardScoring();
        var round   = DebateFactory.EmptyRound();
        var flow    = new FlowGraphBuilder(format, scoring, round).Build(debate);
        return new ScoringEngine().Score(flow, debate, format, scoring, round);
    }
}
