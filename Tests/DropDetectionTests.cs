using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Tests.Helpers;

namespace DebateScoringEngine.Tests;

public static class DropDetectionTests
{
    public static void Run()
    {
        TestRunner.Section("Drop Detection — Basic Cases");

        var format  = DebateFactory.StandardFormat();
        var scoring = DebateFactory.StandardScoring();
        var round   = DebateFactory.EmptyRound();
        var builder = new FlowGraphBuilder(format, scoring, round);

        // 1AC argument with no 1NC response → DROPPED
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms")
                // No 1NC response
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Dropped, graph.GetNode("a1")!.Status,
                "1AC argument with no 1NC response is Dropped");
        }

        // 1AC argument with 1NC response → ACTIVE
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "a1" })
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Active, graph.GetNode("a1")!.Status,
                "1AC argument with 1NC response is Active");
        }

        // 1NC argument with no 2AC response → DROPPED
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "a1" })
                // No 2AC response to n1
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Dropped, graph.GetNode("n1")!.Status,
                "1NC argument with no 2AC response is Dropped");
        }

        // Response in a LATER speech than cutoff does NOT prevent drop
        // 1AC requires answer by 1NC — a 2AC response doesn't count
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),
                DebateFactory.Arg("n1", "2AC", Side.AFF, "Harms", rebuttalTargets: new[] { "a1" })
                // 2AC responds to a1, but the drop rule says 1NC is the cutoff
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Dropped, graph.GetNode("a1")!.Status,
                "Late response (2AC to 1AC arg) does not prevent Dropped status");
        }

        TestRunner.Section("Drop Detection — Multiple Arguments");

        // Some dropped, some not
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),   // answered
                DebateFactory.Arg("a2", "1AC", Side.AFF, "Solvency"), // NOT answered
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "a1" })
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Active,  graph.GetNode("a1")!.Status,
                "a1 answered by n1 → Active");
            TestRunner.AssertEqual(ArgumentStatus.Dropped, graph.GetNode("a2")!.Status,
                "a2 unanswered → Dropped");
            TestRunner.AssertEqual(2, graph.GetDroppedNodes().Count(),
                "Two dropped nodes: a2 (no 1NC) and n1 (no 2AC)");
        }

        TestRunner.Section("Drop Detection — Status Overrides");

        // Human override: status = Active prevents engine marking as Dropped
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    status: ArgumentStatus.Active)  // explicit override
                // No 1NC response, but human says Active
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Active, graph.GetNode("a1")!.Status,
                "Human override keeps Active even without 1NC response");
            TestRunner.Assert(graph.GetNode("a1")!.StatusIsOverridden,
                "StatusIsOverridden flag set when human overrides");
        }

        // Human override: Conceded
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms",
                    status: ArgumentStatus.Conceded)
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Conceded, graph.GetNode("a1")!.Status,
                "Human override to Conceded respected");
        }

        TestRunner.Section("Drop Detection — No Drop Rule for Speech");

        // 2AR has no drop rule (it's the last speech) → never dropped
        {
            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "2AR", Side.AFF, "Harms")
                // 2AR has no drop rule in format
            );
            var graph = builder.Build(debate);
            TestRunner.AssertEqual(ArgumentStatus.Active, graph.GetNode("a1")!.Status,
                "2AR argument has no drop rule → stays Active");
        }

        TestRunner.Section("Flow Graph — Thread Extraction");

        {
            Func<string, int> speechIndex = speechId =>
                DebateFactory.StandardFormat().GetSpeechIndex(speechId);

            var debate = DebateFactory.Debate(
                DebateFactory.Arg("a1", "1AC", Side.AFF, "Harms"),
                DebateFactory.Arg("n1", "1NC", Side.NEG, "Harms", rebuttalTargets: new[] { "a1" }),
                DebateFactory.Arg("a2", "2AC", Side.AFF, "Harms", rebuttalTargets: new[] { "n1" }),
                DebateFactory.Arg("b1", "1AC", Side.AFF, "Solvency")  // separate thread
            );
            var graph = builder.Build(debate);
            var threads = graph.GetThreads(speechIndex).ToList();

            TestRunner.AssertEqual(2, threads.Count, "Two root arguments → two threads");

            var harmsThread = threads.FirstOrDefault(t => t.Root.ArgumentId == "a1");
            TestRunner.Assert(harmsThread != null, "Harms thread has a1 as root");
            TestRunner.AssertEqual(3, harmsThread!.AllNodes.Count, "Harms thread has 3 nodes");

            var solvencyThread = threads.FirstOrDefault(t => t.Root.ArgumentId == "b1");
            TestRunner.Assert(solvencyThread != null, "Solvency thread exists");
            TestRunner.AssertEqual(1, solvencyThread!.AllNodes.Count, "Solvency thread has 1 node");
        }
    }
}
