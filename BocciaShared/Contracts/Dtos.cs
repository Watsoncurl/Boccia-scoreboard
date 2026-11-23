using BocciaShared.Domain;

namespace BocciaShared.Contracts;

/// <summary>Estado completo de un partido: lo que necesitan el control y el marcador.</summary>
public record MatchStateDto(
    int MatchId,
    MatchStatus Status,
    DivisionDto Division,
    string? Court,
    string? Referee,
    SideStateDto Red,
    SideStateDto Blue,
    CurrentEndDto? CurrentEnd,
    IReadOnlyList<EndSummaryDto> Ends,
    bool AwaitingTieBreak,
    Side? WinnerSide,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? FinishedAt);

public record DivisionDto(
    int DivisionId,
    string Code,
    string Name,
    MatchFormat MatchType,
    int PlayersPerSide,
    int BallsPerPlayer,
    int NumberOfEnds,
    int? TimePerEndSeconds)
{
    public int BallsPerSide => PlayersPerSide * BallsPerPlayer;
}

/// <param name="Score">Puntos de los parciales regulares.</param>
/// <param name="TieBreakScore">Puntos en parciales de desempate (no se suman al marcador).</param>
public record SideStateDto(
    Side Side,
    string Name,
    int Score,
    int TieBreakScore,
    IReadOnlyList<SideMemberDto> Members,
    int ColourBallsRemaining,
    int PenaltyBallsAvailable);

public record SideMemberDto(int AthleteId, string FullName, int PlayerNumber, bool IsSubstitute);

public record CurrentEndDto(
    int EndId,
    int EndNumber,
    bool IsTieBreak,
    Side JackSide,
    bool JackThrown,
    IReadOnlyList<ThrowDto> Throws,
    IReadOnlyList<PenaltyDto> Penalties);

public record ThrowDto(
    int ThrowId,
    int Sequence,
    Side Side,
    int? AthleteId,
    BallType BallType,
    ThrowResult Result,
    int? DistanceToJackMm,
    DateTime ThrownAt);

public record PenaltyDto(
    int PenaltyId,
    Side PenalizedSide,
    int? AthleteId,
    PenaltyType PenaltyType,
    string? Reason,
    DateTime CreatedAt);

public record EndSummaryDto(int EndNumber, bool IsTieBreak, EndStatus Status, int RedPoints, int BluePoints);

/// <summary>Resumen para listados de partidos.</summary>
public record MatchSummaryDto(
    int MatchId,
    MatchStatus Status,
    string DivisionCode,
    string? Court,
    string RedName,
    string BlueName,
    int RedScore,
    int BlueScore,
    Side? WinnerSide,
    DateTime? ScheduledAt);

public record AthleteDto(int AthleteId, string FirstName, string LastName, string? Classification, string? Club);
