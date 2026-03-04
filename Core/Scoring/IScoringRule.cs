namespace DebateScoringEngine.Core.Scoring;

/// <summary>
/// Interface all scoring rules implement.
///
/// Design principles:
/// - Rules are additive: each produces independent AFF/NEG scores.
///   The ScoringEngine sums them. Rules do not call or depend on each other.
/// - Rules are stateless: all inputs come through ScoringContext.
///   The same context always produces the same output (deterministic).
/// - Rules are independently testable: pass a ScoringContext, get a RuleResult.
/// - Rules are extensible: add a new rule by implementing this interface
///   and registering it in ScoringEngine. No other code changes needed.
/// </summary>
public interface IScoringRule
{
    /// <summary>Unique identifier — matches keys in scoring-config.json if rule has config.</summary>
    string RuleId { get; }

    /// <summary>Human-readable name shown in score breakdown output.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Evaluates this rule against the entire debate.
    /// Must be deterministic: same context → same result always.
    /// Must not mutate context.
    /// </summary>
    RuleResult Evaluate(ScoringContext context);
}
