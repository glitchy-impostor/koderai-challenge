using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Domain.Models;

public class Speaker
{
    public required string SpeakerId { get; init; }
    public required string Name { get; init; }
    public required Side Side { get; init; }
}
