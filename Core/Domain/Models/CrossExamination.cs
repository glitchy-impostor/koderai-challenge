using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Domain.Models;

/// <summary>
/// A single cross-examination period between two speakers.
/// Always follows a constructive speech.
/// </summary>
public class CrossExamination
{
    public required string CxId { get; init; }

    /// <summary>The speech this CX immediately follows.</summary>
    public required string AfterSpeechId { get; init; }

    public required string ExaminerId { get; init; }
    public required string RespondentId { get; init; }
    public required Side ExaminerSide { get; init; }
    public required Side RespondentSide { get; init; }

    public int TimeAllocatedSeconds { get; init; }
    public int TimeUsedSeconds { get; init; }

    public List<CxQuestion> Questions { get; init; } = new();
}
