using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Core.Scoring;
using DebateScoringEngine.Core.Scoring.Rules;
using DebateScoringEngine.Tests.Helpers;

namespace DebateScoringEngine.Tests;

public static class ScoringRuleTests
{
    public static void Run()
    {
        // ── ArgumentStrengthRule ──────────────────────────────────────────────
        TestRunner.Section("ArgumentStrengthRule");

        {
            // Two active AFF args, one active NEG arg
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant), // 3.0
                DebateFactory.Arg("a2", "1AC", Side.AFF, "Solvency",
                    evidence: EvidenceQuality.ExpertOpinion, impact: ImpactMagnitude.Minor),       // 1.7
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.NewsSource, impact: ImpactMagnitude.Significant)     // 2.1
            );
            var ctx = BuildContext(debate);
            var result = new ArgumentStrengthRule().Evaluate(ctx);

            TestRunner.Assert(result.AffScore > result.NegScore,
                "ArgStrength: AFF scores higher with stronger arguments");
            // a2 dropped (no 1NC answer) and n1 dropped (no 2AC answer) → only a1 is Active
            TestRunner.AssertEqual(1, result.ArgumentDetails.Count,
                "ArgStrength: only 1 active argument (a2 + n1 are dropped)");
        }

        {
            // Dropped argument excluded from strength scoring
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant,
                    status: ArgumentStatus.Dropped) // explicitly dropped
            );
            var ctx = BuildContext(debate);
            var result = new ArgumentStrengthRule().Evaluate(ctx);
            TestRunner.AssertEqual(0.0, result.AffScore,
                "ArgStrength: Dropped argument contributes 0 strength");
            TestRunner.AssertEqual(0, result.ArgumentDetails.Count,
                "ArgStrength: Dropped argument has no detail entry");
        }

        // ── DroppedArgumentRule ───────────────────────────────────────────────
        TestRunner.Section("DroppedArgumentRule");

        {
            // Engine-derived dropped arg (no 1NC response)
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant)
                // No 1NC response → engine marks as Dropped
            );
            var ctx = BuildContext(debate);
            var result = new DroppedArgumentRule().Evaluate(ctx);

            // a1 strength = 3.0; multiplier = 1.5 → score = 4.5 for AFF
            TestRunner.Assert(result.AffScore > 0,
                "DroppedArg: AFF gets bonus for NEG dropping their argument");
            TestRunner.Assert(Math.Abs(result.AffScore - 4.5) < 0.001,
                "DroppedArg: AFF bonus = strength(3.0) × multiplier(1.5) = 4.5");
            TestRunner.AssertEqual(0.0, result.NegScore,
                "DroppedArg: NEG gets no bonus (they dropped the arg, not AFF)");
        }

        {
            // NEG arg dropped by AFF
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant),
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant)
                // No 2AC response to n1 → Dropped
            );
            var ctx = BuildContext(debate);
            var result = new DroppedArgumentRule().Evaluate(ctx);
            TestRunner.Assert(result.NegScore > 0,
                "DroppedArg: NEG gets bonus for AFF dropping their argument");
        }

        // ── LogicalConsistencyRule ────────────────────────────────────────────
        TestRunner.Section("LogicalConsistencyRule");

        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    fallacies: new() { FallacyType.StrawMan, FallacyType.AdHominem })
                // StrawMan=0.5 + AdHominem=0.75 = 1.25 penalty on AFF
            );
            var ctx = BuildContext(debate);
            var result = new LogicalConsistencyRule().Evaluate(ctx);
            TestRunner.Assert(result.AffScore < 0,
                "LogicalConsistency: AFF penalised for fallacies");
            TestRunner.Assert(Math.Abs(result.AffScore - (-1.25)) < 0.001,
                "LogicalConsistency: AFF penalty = -(StrawMan + AdHominem) = -1.25");
            TestRunner.AssertEqual(0.0, result.NegScore,
                "LogicalConsistency: NEG has no fallacies, no penalty");
        }

        {
            // No fallacies → zero score
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant)
            );
            var ctx = BuildContext(debate);
            var result = new LogicalConsistencyRule().Evaluate(ctx);
            TestRunner.AssertEqual(0.0, result.AffScore,
                "LogicalConsistency: No fallacies → zero penalty");
        }

        // ── RebuttalEffectivenessRule ─────────────────────────────────────────
        TestRunner.Section("RebuttalEffectivenessRule");

        {
            // NEG rebuts AFF with equal strength → effectiveness = 1.0
            // score = 1.0 × targetStrength(3.0) × weight(0.4) = 1.2
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant), // 3.0
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms",
                    rebuttalTargets: new[] { "a1" },
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant)  // 3.0
            );
            var ctx = BuildContext(debate);
            var result = new RebuttalEffectivenessRule().Evaluate(ctx);
            TestRunner.Assert(Math.Abs(result.NegScore - 1.2) < 0.001,
                "RebuttalEff: Equal strength rebuttal scores 1.0 × 3.0 × 0.4 = 1.2");
        }

        {
            // No rebuttals → zero scores
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    evidence: EvidenceQuality.PeerReviewed, impact: ImpactMagnitude.Significant)
            );
            var ctx = BuildContext(debate);
            var result = new RebuttalEffectivenessRule().Evaluate(ctx);
            TestRunner.AssertEqual(0.0, result.AffScore,
                "RebuttalEff: No rebuttals → 0 score");
            TestRunner.AssertEqual(0.0, result.NegScore,
                "RebuttalEff: No rebuttals → 0 neg score");
        }

        // ── TimeEfficiencyRule ────────────────────────────────────────────────
        TestRunner.Section("TimeEfficiencyRule");

        {
            // Over time: 1AC allocated 480, used 500 → 20s over → penalty = 20 × 0.01 = 0.2
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")
            );
            // Patch speech time
            var speech = debate.Speeches.First(s => s.SpeechId == "1AC");
            var patchedDebate = PatchSpeechTime(debate, "1AC", allocated: 480, used: 500);
            var ctx = BuildContext(patchedDebate);
            var result = new TimeEfficiencyRule().Evaluate(ctx);
            TestRunner.Assert(result.AffScore < 0,
                "TimeEff: Over-time speech penalises AFF");
            TestRunner.Assert(Math.Abs(result.AffScore - (-0.2)) < 0.001,
                "TimeEff: 20s over × 0.01 = -0.2 penalty");
        }

        {
            // Under time: 1AC allocated 480, used 300 → ratio=62.5% < 75% → -0.5
            var patchedDebate = PatchSpeechTime(
                DebateFactory.Debate(DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")),
                "1AC", allocated: 480, used: 300);
            var ctx = BuildContext(patchedDebate);
            var result = new TimeEfficiencyRule().Evaluate(ctx);
            TestRunner.Assert(Math.Abs(result.AffScore - (-0.5)) < 0.001,
                "TimeEff: Under-time speech (62.5%) → -0.5 flat penalty");
        }

        // ── CrossExaminationRule ──────────────────────────────────────────────
        TestRunner.Section("CrossExaminationRule");

        {
            var debate = BuildDebateWithCX(admissions: 2, evasions: 1,
                examinerSide: Side.NEG, respondentSide: Side.AFF);
            var ctx = BuildContext(debate);
            var result = new CrossExaminationRule().Evaluate(ctx);

            // NEG examiner extracts 2 admissions: +0.6
            TestRunner.Assert(Math.Abs(result.NegScore - 0.6) < 0.001,
                "CX: NEG gets 2 × 0.3 = 0.6 for admissions");
            // AFF respondent gives 1 evasive answer: -0.2
            TestRunner.Assert(Math.Abs(result.AffScore - (-0.2)) < 0.001,
                "CX: AFF gets -0.2 for 1 evasion");
        }

        // ── PrepTimeRule ──────────────────────────────────────────────────────
        TestRunner.Section("PrepTimeRule");

        {
            // AFF uses 200 of 480 → 280 unused → +0.28 bonus
            // NEG uses 415 of 480 → 65 unused → +0.065 bonus
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")
            );
            // PrepTimeUsedSeconds already set to AFF=200, NEG=250 in factory
            var ctx = BuildContext(debate);
            var result = new PrepTimeRule().Evaluate(ctx);
            TestRunner.Assert(result.AffScore > result.NegScore,
                "PrepTime: AFF used less prep → higher bonus than NEG");
            // AFF: 480-200=280 × 0.001 = 0.28
            TestRunner.Assert(Math.Abs(result.AffScore - 0.28) < 0.001,
                "PrepTime: AFF bonus = 280 × 0.001 = 0.28");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ScoringContext BuildContext(DebateScoringEngine.Core.Domain.Models.Debate debate)
    {
        var format  = DebateFactory.StandardFormat();
        var scoring = DebateFactory.StandardScoring();
        var round   = DebateFactory.EmptyRound();
        var flow    = new FlowGraphBuilder(format, scoring, round).Build(debate);
        return new ScoringContext
        {
            Flow    = flow,
            Debate  = debate,
            Format  = format,
            Scoring = scoring,
            Round   = round
        };
    }

    private static DebateScoringEngine.Core.Domain.Models.Debate PatchSpeechTime(
        DebateScoringEngine.Core.Domain.Models.Debate debate,
        string speechId, int allocated, int used)
    {
        var speeches = debate.Speeches.Select(s =>
            s.SpeechId == speechId
                ? new DebateScoringEngine.Core.Domain.Models.Speech
                {
                    SpeechId             = s.SpeechId,
                    SpeakerId            = s.SpeakerId,
                    Side                 = s.Side,
                    TimeAllocatedSeconds = allocated,
                    TimeUsedSeconds      = used,
                    ArgumentIds          = s.ArgumentIds,
                }
                : s).ToList();

        return new DebateScoringEngine.Core.Domain.Models.Debate
        {
            DebateId            = debate.DebateId,
            RoundId             = debate.RoundId,
            Teams               = debate.Teams,
            Speakers            = debate.Speakers,
            Speeches            = speeches,
            Arguments           = debate.Arguments,
            CrossExaminations   = debate.CrossExaminations,
            PrepTimeUsedSeconds = debate.PrepTimeUsedSeconds,
        };
    }

    private static DebateScoringEngine.Core.Domain.Models.Debate BuildDebateWithCX(
        int admissions, int evasions,
        Side examinerSide, Side respondentSide)
    {
        var debate = DebateFactory.Debate(
            DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")
        );

        var questions = new List<DebateScoringEngine.Core.Domain.Models.CxQuestion>();
        for (int i = 0; i < admissions; i++)
            questions.Add(new() { QuestionId = $"q-admit-{i}", AdmissionExtracted = true });
        for (int i = 0; i < evasions; i++)
            questions.Add(new() { QuestionId = $"q-evasion-{i}", Evasive = true });

        var cx = new DebateScoringEngine.Core.Domain.Models.CrossExamination
        {
            CxId                 = "cx-1",
            AfterSpeechId        = "1AC",
            ExaminerId           = "spk-neg",
            RespondentId         = "spk-aff",
            ExaminerSide         = examinerSide,
            RespondentSide       = respondentSide,
            TimeAllocatedSeconds = 180,
            TimeUsedSeconds      = 170,
            Questions            = questions,
        };

        return new DebateScoringEngine.Core.Domain.Models.Debate
        {
            DebateId            = debate.DebateId,
            RoundId             = debate.RoundId,
            Teams               = debate.Teams,
            Speakers            = debate.Speakers,
            Speeches            = debate.Speeches,
            Arguments           = debate.Arguments,
            CrossExaminations   = new() { cx },
            PrepTimeUsedSeconds = debate.PrepTimeUsedSeconds,
        };
    }
}
