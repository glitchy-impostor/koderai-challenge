using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.Domain.Models;

public class Team
{
    public required string TeamId { get; init; }
    public required Side Side { get; init; }
    public required List<string> SpeakerIds { get; init; }
}
