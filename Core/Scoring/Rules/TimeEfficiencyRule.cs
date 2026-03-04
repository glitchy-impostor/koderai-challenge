using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Scores time usage efficiency per speech.
///
/// Over time: flat penalty per second over the allocated time.
///   This reflects that going overtime is a rules violation in competitive debate.
///
/// Under time: flat penalty when a speaker uses less than the threshold percentage.
///   Using far less than your time suggests insufficient material — a strategic weakness.
///   Minor underage (e.g., 78% of time) incurs no penalty; severe underage does.
///
/// Both sides are scored independently across their speeches.
/// Score is per-side (not per-argument) — attached to a synthetic "speech" detail entry.
///
/// Config:
///   overTimePenaltyPerSecond — penalty per second over limit (default 0.01)
///   underTimeThresholdPercent — threshold below which penalty applies (default 0.75)
///   underTimePenalty — flat penalty for being under threshold (default 0.5)
/// </summary>
public class TimeEfficiencyRule : IScoringRule
{
    public string RuleId      => "time-efficiency";
    public string DisplayName => "Time Efficiency";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details  = new List<ArgumentScoreDetail>();
        double affPenalty = 0, negPenalty = 0;
        var cfg = context.Scoring.TimeEfficiency;

        foreach (var speech in context.Debate.Speeches)
        {
            // Skip cross-examination periods — they have different time dynamics
            var speechDef = context.Format.GetSpeech(speech.SpeechId);
            if (speechDef == null || speechDef.Type == "CrossEx") continue;

            var allocated = speech.TimeAllocatedSeconds;
            var used      = speech.TimeUsedSeconds;
            if (allocated <= 0) continue;

            double penalty = 0;
            string note;

            if (used > allocated)
            {
                var overBy = used - allocated;
                penalty = overBy * cfg.OverTimePenaltyPerSecond;
                note    = $"Over time by {overBy}s → -{penalty:F2}";
            }
            else
            {
                var ratio = (double)used / allocated;
                if (ratio < cfg.UnderTimeThresholdPercent)
                {
                    penalty = cfg.UnderTimePenalty;
                    note    = $"Under time ({ratio:P0} of {allocated}s used) → -{penalty:F2}";
                }
                else
                {
                    note = $"Time OK ({ratio:P0} of {allocated}s used)";
                }
            }

            if (penalty > 0)
            {
                if (speech.Side == Side.AFF) affPenalty += penalty;
                else                         negPenalty += penalty;

                details.Add(new ArgumentScoreDetail
                {
                    ArgumentId    = $"speech:{speech.SpeechId}",
                    SpeechId      = speech.SpeechId,
                    StockIssueTag = "TimeEfficiency",
                    Side          = speech.Side.ToString(),
                    SpeakerId     = speech.SpeakerId,
                    RuleId        = RuleId,
                    Score         = -penalty,
                    Note          = $"[{speech.SpeechId}] {note}"
                });
            }
        }

        return new RuleResult
        {
            RuleId          = RuleId,
            DisplayName     = DisplayName,
            AffScore        = -affPenalty,
            NegScore        = -negPenalty,
            ArgumentDetails = details,
            Explanation     = BuildExplanation(affPenalty, negPenalty, details)
        };
    }

    private static string BuildExplanation(double affPenalty, double negPenalty,
        List<ArgumentScoreDetail> details)
    {
        if (details.Count == 0)
            return "Time Efficiency: Both sides used their time within acceptable bounds.";

        return $"Time Efficiency: {details.Count} speech(es) had time violations. " +
               $"AFF time penalties: -{affPenalty:F2}; NEG time penalties: -{negPenalty:F2}.";
    }
}
