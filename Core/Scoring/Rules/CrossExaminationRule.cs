using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Scoring.Rules;

/// <summary>
/// Scores cross-examination performance.
/// CX scoring is fully deterministic — based on structured boolean metadata only.
///
/// Three dimensions:
///
/// 1. Admissions extracted (bonus to EXAMINER's side):
///    Each question where admissionExtracted=true awards the examiner's side a bonus.
///    Reflects that extracting a damaging concession is a strategic win.
///
/// 2. Evasive answers (penalty to RESPONDENT's side):
///    Each question where evasive=true penalizes the respondent's side.
///    Reflects that failing to directly answer undermines credibility.
///
/// 3. Time efficiency (minor penalty for extreme underage):
///    CX periods where less than 50% of time was used get a small flat penalty to the examiner.
///    Reflects failure to probe effectively.
///
/// Config:
///   cxAdmissionBonus — per admission extracted (default 0.3)
///   cxEvasionPenalty — per evasive answer (default 0.2)
/// </summary>
public class CrossExaminationRule : IScoringRule
{
    public string RuleId      => "cross-examination";
    public string DisplayName => "Cross-Examination";

    public RuleResult Evaluate(ScoringContext context)
    {
        var details = new List<ArgumentScoreDetail>();
        double affScore = 0, negScore = 0;

        var admissionBonus = context.Scoring.CxAdmissionBonus;
        var evasionPenalty = context.Scoring.CxEvasionPenalty;

        foreach (var cx in context.Debate.CrossExaminations)
        {
            foreach (var question in cx.Questions)
            {
                // Admission bonus → examiner's side
                if (question.AdmissionExtracted)
                {
                    if (cx.ExaminerSide == Side.AFF) affScore += admissionBonus;
                    else                              negScore += admissionBonus;

                    details.Add(new ArgumentScoreDetail
                    {
                        ArgumentId    = $"cx:{cx.CxId}:{question.QuestionId}:admission",
                        SpeechId      = $"CX after {cx.AfterSpeechId}",
                        StockIssueTag = "CrossEx",
                        Side          = cx.ExaminerSide.ToString(),
                        SpeakerId     = cx.ExaminerId,
                        RuleId        = RuleId,
                        Score         = admissionBonus,
                        Note          = $"Admission extracted by {cx.ExaminerSide}" +
                                        (question.AdmissionNote != null
                                            ? $": \"{question.AdmissionNote}\""
                                            : string.Empty)
                    });
                }

                // Evasion penalty → respondent's side
                if (question.Evasive)
                {
                    if (cx.RespondentSide == Side.AFF) affScore -= evasionPenalty;
                    else                               negScore -= evasionPenalty;

                    details.Add(new ArgumentScoreDetail
                    {
                        ArgumentId    = $"cx:{cx.CxId}:{question.QuestionId}:evasion",
                        SpeechId      = $"CX after {cx.AfterSpeechId}",
                        StockIssueTag = "CrossEx",
                        Side          = cx.RespondentSide.ToString(),
                        SpeakerId     = cx.RespondentId,
                        RuleId        = RuleId,
                        Score         = -evasionPenalty,
                        Note          = $"Evasive answer by {cx.RespondentSide} → -{evasionPenalty:F2}"
                    });
                }
            }

            // CX time efficiency — penalise examiner for severely under-using CX time
            if (cx.TimeAllocatedSeconds > 0)
            {
                var ratio = (double)cx.TimeUsedSeconds / cx.TimeAllocatedSeconds;
                if (ratio < 0.5)
                {
                    var penalty = 0.25; // flat, small — failure to probe
                    if (cx.ExaminerSide == Side.AFF) affScore -= penalty;
                    else                              negScore -= penalty;

                    details.Add(new ArgumentScoreDetail
                    {
                        ArgumentId    = $"cx:{cx.CxId}:time",
                        SpeechId      = $"CX after {cx.AfterSpeechId}",
                        StockIssueTag = "CrossEx",
                        Side          = cx.ExaminerSide.ToString(),
                        SpeakerId     = cx.ExaminerId,
                        RuleId        = RuleId,
                        Score         = -penalty,
                        Note          = $"CX examiner used only {ratio:P0} of allocated time → -{penalty:F2}"
                    });
                }
            }
        }

        return new RuleResult
        {
            RuleId          = RuleId,
            DisplayName     = DisplayName,
            AffScore        = affScore,
            NegScore        = negScore,
            ArgumentDetails = details,
            Explanation     = BuildExplanation(affScore, negScore, details)
        };
    }

    private static string BuildExplanation(double aff, double neg, List<ArgumentScoreDetail> details)
    {
        if (details.Count == 0)
            return "Cross-Examination: No notable CX events (no admissions, no evasions).";

        var admissions = details.Count(d => d.ArgumentId.EndsWith(":admission"));
        var evasions   = details.Count(d => d.ArgumentId.EndsWith(":evasion"));
        return $"Cross-Examination: {admissions} admission(s) extracted, {evasions} evasion(s) noted. " +
               $"AFF CX score: {aff:F2}; NEG CX score: {neg:F2}.";
    }
}
