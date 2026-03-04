namespace DebateScoringEngine.Core.FlowGraph;

/// <summary>
/// A directed edge in the flow graph representing a rebuttal relationship.
/// Direction: Source argument is responding to / attacking the Target argument.
///
/// Created from Argument.RebuttalTargetIds during graph construction.
/// Multiple edges can share the same source (one argument addresses multiple targets).
/// Multiple edges can share the same target (multiple arguments respond to the same claim).
/// </summary>
public class RebuttalEdge
{
    /// <summary>The argumentId of the argument making the rebuttal.</summary>
    public string SourceArgumentId { get; }

    /// <summary>The speechId of the source argument (used in drop detection).</summary>
    public string SourceSpeechId { get; }

    /// <summary>The argumentId being rebutted.</summary>
    public string TargetArgumentId { get; }

    /// <summary>The speechId of the target argument.</summary>
    public string TargetSpeechId { get; }

    public RebuttalEdge(
        string sourceArgumentId,
        string sourceSpeechId,
        string targetArgumentId,
        string targetSpeechId)
    {
        SourceArgumentId = sourceArgumentId;
        SourceSpeechId   = sourceSpeechId;
        TargetArgumentId = targetArgumentId;
        TargetSpeechId   = targetSpeechId;
    }

    public override string ToString() =>
        $"{SourceArgumentId} ({SourceSpeechId}) → {TargetArgumentId} ({TargetSpeechId})";
}
