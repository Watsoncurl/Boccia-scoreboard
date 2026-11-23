using BocciaServer.Data;
using BocciaShared.Contracts;
using BocciaShared.Domain;

namespace BocciaServer.Services;

public static class MatchMapper
{
    public static DivisionDto ToDto(this Division d) =>
        new(d.DivisionId, d.Code, d.Name, d.MatchType, d.PlayersPerSide, d.BallsPerPlayer, d.NumberOfEnds, d.TimePerEndSeconds);

    public static AthleteDto ToDto(this Athlete a) =>
        new(a.AthleteId, a.FirstName, a.LastName, a.Classification, a.Club);

    /// <summary>Requiere el partido cargado con división, lados, miembros, parciales, lanzamientos y sanciones.</summary>
    public static MatchStateDto ToStateDto(this Match match)
    {
        var current = match.CurrentEnd();

        return new MatchStateDto(
            match.MatchId,
            match.Status,
            match.Division.ToDto(),
            match.Court,
            match.Referee,
            SideState(match, Side.Red, current),
            SideState(match, Side.Blue, current),
            current is null ? null : new CurrentEndDto(
                current.EndId,
                current.EndNumber,
                current.IsTieBreak,
                current.JackSide,
                current.Throws.Any(t => t.BallType == BallType.Jack),
                current.Throws.OrderBy(t => t.Sequence)
                    .Select(t => new ThrowDto(t.ThrowId, t.Sequence, t.Side, t.AthleteId, t.BallType, t.Result, t.DistanceToJackMm, t.ThrownAt))
                    .ToList(),
                current.Penalties.OrderBy(p => p.CreatedAt)
                    .Select(p => new PenaltyDto(p.PenaltyId, p.PenalizedSide, p.AthleteId, p.PenaltyType, p.Reason, p.CreatedAt))
                    .ToList()),
            match.Ends.OrderBy(e => e.EndNumber)
                .Select(e => new EndSummaryDto(e.EndNumber, e.IsTieBreak, e.Status, e.RedPoints, e.BluePoints))
                .ToList(),
            match.IsAwaitingTieBreak(),
            match.WinnerSide,
            match.ScheduledAt,
            match.StartedAt,
            match.FinishedAt);
    }

    public static MatchSummaryDto ToSummaryDto(this Match match)
    {
        var (red, blue) = match.RegularScore();
        return new MatchSummaryDto(
            match.MatchId,
            match.Status,
            match.Division.Code,
            match.Court,
            match.SideOf(Side.Red).DisplayName,
            match.SideOf(Side.Blue).DisplayName,
            red,
            blue,
            match.WinnerSide,
            match.ScheduledAt);
    }

    private static SideStateDto SideState(Match match, Side side, End? current)
    {
        var matchSide = match.SideOf(side);
        var finishedEnds = match.Ends.Where(e => e.Status == EndStatus.Finished).ToList();

        return new SideStateDto(
            side,
            matchSide.DisplayName,
            finishedEnds.Where(e => !e.IsTieBreak).Sum(e => e.PointsOf(side)),
            finishedEnds.Where(e => e.IsTieBreak).Sum(e => e.PointsOf(side)),
            matchSide.Members.OrderBy(m => m.IsSubstitute).ThenBy(m => m.PlayerNumber)
                .Select(m => new SideMemberDto(m.AthleteId, m.Athlete.FullName, m.PlayerNumber, m.IsSubstitute))
                .ToList(),
            current is null ? 0 : match.Division.BallsPerSide - current.ColourBallsThrownBy(side),
            current?.PenaltyBallsAvailableFor(side) ?? 0);
    }
}

/// <summary>Consultas de dominio sobre las entidades cargadas.</summary>
public static class MatchRules
{
    public static MatchSide SideOf(this Match match, Side side) =>
        match.Sides.Single(s => s.Side == side);

    public static End? CurrentEnd(this Match match) =>
        match.Ends.Where(e => e.Status == EndStatus.InProgress).MaxBy(e => e.EndNumber);

    public static int PointsOf(this End end, Side side) =>
        side == Side.Red ? end.RedPoints : end.BluePoints;

    public static (int Red, int Blue) RegularScore(this Match match)
    {
        var regular = match.Ends.Where(e => e.Status == EndStatus.Finished && !e.IsTieBreak).ToList();
        return (regular.Sum(e => e.RedPoints), regular.Sum(e => e.BluePoints));
    }

    public static int FinishedRegularEnds(this Match match) =>
        match.Ends.Count(e => e.Status == EndStatus.Finished && !e.IsTieBreak);

    /// <summary>Partido en juego, parciales regulares completos, empatado y sin parcial abierto.</summary>
    public static bool IsAwaitingTieBreak(this Match match) =>
        match.Status == MatchStatus.InProgress
        && match.CurrentEnd() is null
        && match.FinishedRegularEnds() >= match.Division.NumberOfEnds;

    public static int ColourBallsThrownBy(this End end, Side side) =>
        end.Throws.Count(t => t.Side == side && t.BallType == BallType.Colour);

    /// <summary>Bolas de penalización ganadas por <paramref name="side"/> (sanciones del rival) aún no lanzadas.</summary>
    public static int PenaltyBallsAvailableFor(this End end, Side side) =>
        end.Penalties.Count(p => p.PenaltyType == PenaltyType.PenaltyBall && p.PenalizedSide == side.Opponent())
        - end.Throws.Count(t => t.Side == side && t.BallType == BallType.Penalty);
}
