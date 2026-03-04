namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// A single question-answer exchange within a cross-examination period.
/// Scoring is fully deterministic — based on structured boolean/string fields only.
/// </summary>
public class CxQuestion
{
    public required string QuestionId { get; init; }

    /// <summary>The argument this question is targeting or probing.</summary>
    public string? TargetArgumentId { get; init; }

    /// <summary>
    /// True if the respondent evaded the question without substantive answer.
    /// Contributes a penalty to the respondent's CX score.
    /// </summary>
    public bool Evasive { get; init; }

    /// <summary>
    /// True if the examiner successfully extracted a damaging admission.
    /// Contributes a bonus to the examiner's CX score.
    /// </summary>
    public bool AdmissionExtracted { get; init; }

    /// <summary>Optional note describing the admission or evasion for explanation output.</summary>
    public string? AdmissionNote { get; init; }
}
