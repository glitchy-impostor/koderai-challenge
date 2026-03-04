using DebateScoringEngine.Tests;

Console.WriteLine("=== Debate Scoring Engine — Test Suite ===\n");

FlowGraphBuilderTests.Run();
DropDetectionTests.Run();
ScoringRuleTests.Run();
ScoringEngineIntegrationTests.Run();

return TestRunner.Report();
