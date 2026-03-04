namespace DebateScoringEngine.Tests;

/// <summary>
/// Minimal test runner — no xunit or external packages required.
/// Runs all test classes, reports pass/fail with details.
/// </summary>
public static class TestRunner
{
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new();

    public static void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine($"  ✓ {testName}");
        }
        else
        {
            _failed++;
            var msg = $"  ✗ FAIL: {testName}";
            Failures.Add(msg);
            Console.WriteLine(msg);
        }
    }

    public static void AssertEqual<T>(T expected, T actual, string testName)
    {
        var ok = Equals(expected, actual);
        if (!ok)
        {
            _failed++;
            var msg = $"  ✗ FAIL: {testName} — expected [{expected}] got [{actual}]";
            Failures.Add(msg);
            Console.WriteLine(msg);
        }
        else
        {
            _passed++;
            Console.WriteLine($"  ✓ {testName}");
        }
    }

    public static void AssertThrows<TException>(Action action, string testName)
        where TException : Exception
    {
        try
        {
            action();
            _failed++;
            var msg = $"  ✗ FAIL: {testName} — expected {typeof(TException).Name} but no exception thrown";
            Failures.Add(msg);
            Console.WriteLine(msg);
        }
        catch (TException)
        {
            _passed++;
            Console.WriteLine($"  ✓ {testName}");
        }
        catch (Exception ex)
        {
            _failed++;
            var msg = $"  ✗ FAIL: {testName} — expected {typeof(TException).Name} but got {ex.GetType().Name}";
            Failures.Add(msg);
            Console.WriteLine(msg);
        }
    }

    public static void Section(string name)
    {
        Console.WriteLine($"\n── {name} ──");
    }

    public static int Report()
    {
        Console.WriteLine($"\n{'─',40}");
        Console.WriteLine($"Results: {_passed} passed, {_failed} failed");
        if (Failures.Count > 0)
        {
            Console.WriteLine("\nFailed tests:");
            Failures.ForEach(f => Console.WriteLine(f));
        }
        return _failed == 0 ? 0 : 1;
    }
}
