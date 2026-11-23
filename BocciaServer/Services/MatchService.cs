using BocciaServer.Data;
using BocciaShared.Contracts;
using BocciaShared.Domain;
using Microsoft.EntityFrameworkCore;

namespace BocciaServer.Services;

/// <summary>
/// Ciclo de vida de un partido: creación, inicio, lanzamientos, sanciones,
/// cierre de parciales, desempate y finalización. Cada cambio se guarda en la
/// base de datos y se notifica a los clientes por SignalR.
/// </summary>
public class MatchService(BocciaDbContext db, IMatchNotifier notifier, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // ---------------------------------------------------------------- Consultas

    public async Task<MatchStateDto> GetStateAsync(int matchId, CancellationToken ct = default) =>
        (await LoadAsync(matchId, ct)).ToStateDto();

    public async Task<IReadOnlyList<MatchSummaryDto>> ListAsync(MatchStatus? status, CancellationToken ct = default)
    {
        var query = db.Matches
            .Include(m => m.Division)
            .Include(m => m.Sides)
            .Include(m => m.Ends)
            .AsSplitQuery()
            .AsNoTracking();

        if (status is not null) query = query.Where(m => m.Status == status);

        var matches = await query.OrderByDescending(m => m.MatchId).ToListAsync(ct);
        return matches.Select(m => m.ToSummaryDto()).ToList();
    }

    // ---------------------------------------------------------------- Partido

    public async Task<MatchStateDto> CreateAsync(CreateMatchRequest request, CancellationToken ct = default)
    {
        var division = await db.Divisions.SingleOrDefaultAsync(d => d.Code == request.DivisionCode, ct)
            ?? throw new NotFoundException($"No existe la división '{request.DivisionCode}'.");

        var allIds = request.Red.AthleteIds.Concat(request.Red.SubstituteIds)
            .Concat(request.Blue.AthleteIds).Concat(request.Blue.SubstituteIds)
            .ToList();

        if (allIds.Count != allIds.Distinct().Count())
            throw new GameRuleException("Un deportista no puede figurar más de una vez en el partido.");

        var athletes = await db.Athletes.Where(a => allIds.Contains(a.AthleteId)).ToDictionaryAsync(a => a.AthleteId, ct);
        var missing = allIds.Where(id => !athletes.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"No existen los deportistas: {string.Join(", ", missing)}.");

        var match = new Match
        {
            Division = division,
            Court = request.Court,
            Referee = request.Referee,
            ScheduledAt = request.ScheduledAt,
            Status = MatchStatus.Scheduled,
            Sides =
            [
                BuildSide(Side.Red, request.Red, division, athletes),
                BuildSide(Side.Blue, request.Blue, division, athletes),
            ],
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync(ct);
        return await PublishAsync(match.MatchId, ct);
    }

    public async Task<MatchStateDto> StartAsync(int matchId, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        if (match.Status != MatchStatus.Scheduled)
            throw new GameRuleException("Solo se puede iniciar un partido programado.");

        match.Status = MatchStatus.InProgress;
        match.StartedAt = Now;
        // Reglamento: el lado rojo lanza el jack en el primer parcial.
        OpenEnd(match, endNumber: 1, jackSide: Side.Red, isTieBreak: false);

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    public async Task<MatchStateDto> CancelAsync(int matchId, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        if (match.Status is MatchStatus.Finished or MatchStatus.Cancelled)
            throw new GameRuleException("El partido ya terminó o fue cancelado.");

        if (match.CurrentEnd() is { } current)
        {
            current.Status = EndStatus.Annulled;
            current.FinishedAt = Now;
        }

        match.Status = MatchStatus.Cancelled;
        match.FinishedAt = Now;

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    // ---------------------------------------------------------------- Lanzamientos

    public async Task<MatchStateDto> RegisterThrowAsync(int matchId, RegisterThrowRequest request, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        var end = RequireCurrentEnd(match);
        var side = request.Side!.Value;
        var jackThrown = end.Throws.Any(t => t.BallType == BallType.Jack);

        switch (request.BallType)
        {
            case BallType.Jack:
                if (jackThrown)
                    throw new GameRuleException("El jack ya fue lanzado en este parcial.");
                if (side != end.JackSide)
                    throw new GameRuleException($"En este parcial el jack lo lanza el lado {end.JackSide.ToSpanish()}.");
                if (request.DistanceToJackMm is not null)
                    throw new GameRuleException("El jack no tiene distancia al jack.");
                break;

            case BallType.Colour:
                if (!jackThrown)
                    throw new GameRuleException("Primero debe lanzarse el jack.");
                if (end.ColourBallsThrownBy(side) >= match.Division.BallsPerSide)
                    throw new GameRuleException($"El lado {side.ToSpanish()} ya lanzó sus {match.Division.BallsPerSide} bolas.");
                break;

            case BallType.Penalty:
                if (!jackThrown)
                    throw new GameRuleException("Primero debe lanzarse el jack.");
                if (end.PenaltyBallsAvailableFor(side) <= 0)
                    throw new GameRuleException($"El lado {side.ToSpanish()} no tiene bolas de penalización disponibles.");
                break;
        }

        if (request.AthleteId is { } athleteId)
        {
            EnsureAthleteInSide(match, side, athleteId);

            if (request.BallType == BallType.Colour)
            {
                var thrownByAthlete = end.Throws.Count(t => t.AthleteId == athleteId && t.BallType == BallType.Colour);
                if (thrownByAthlete >= match.Division.BallsPerPlayer)
                    throw new GameRuleException($"El deportista ya lanzó sus {match.Division.BallsPerPlayer} bolas en este parcial.");
            }
        }

        end.Throws.Add(new Throw
        {
            Sequence = (byte)(end.Throws.Count == 0 ? 1 : end.Throws.Max(t => t.Sequence) + 1),
            Side = side,
            AthleteId = request.AthleteId,
            BallType = request.BallType,
            Result = request.Result,
            DistanceToJackMm = request.DistanceToJackMm,
            ThrownAt = Now,
        });

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    /// <summary>Corrige el resultado o la distancia de un lanzamiento del parcial en juego.</summary>
    public async Task<MatchStateDto> UpdateThrowAsync(int matchId, int throwId, UpdateThrowRequest request, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        var end = RequireCurrentEnd(match);
        var thrown = FindThrow(end, throwId);

        ApplyMeasurement(thrown, request.Result, request.DistanceToJackMm);

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    /// <summary>Deshace el último lanzamiento del parcial en juego (corrección de errores de carga).</summary>
    public async Task<MatchStateDto> UndoLastThrowAsync(int matchId, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        var end = RequireCurrentEnd(match);
        var last = end.Throws.MaxBy(t => t.Sequence)
            ?? throw new GameRuleException("No hay lanzamientos para deshacer en este parcial.");

        end.Throws.Remove(last);
        db.Throws.Remove(last);

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    // ---------------------------------------------------------------- Sanciones

    public async Task<MatchStateDto> AddPenaltyAsync(int matchId, AddPenaltyRequest request, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        var end = RequireCurrentEnd(match);
        var side = request.PenalizedSide!.Value;

        if (request.AthleteId is { } athleteId) EnsureAthleteInSide(match, side, athleteId);

        end.Penalties.Add(new Penalty
        {
            PenalizedSide = side,
            AthleteId = request.AthleteId,
            PenaltyType = request.PenaltyType!.Value,
            Reason = request.Reason,
            CreatedAt = Now,
        });

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    // ---------------------------------------------------------------- Parciales

    /// <summary>
    /// Cierra el parcial en juego: aplica las mediciones, calcula los puntos
    /// (o usa el puntaje manual del árbitro) y avanza el partido: abre el
    /// siguiente parcial, lo da por terminado o queda esperando un desempate.
    /// </summary>
    public async Task<MatchStateDto> FinishEndAsync(int matchId, FinishEndRequest request, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        var end = RequireCurrentEnd(match);

        foreach (var m in request.Measurements)
            ApplyMeasurement(FindThrow(end, m.ThrowId), m.Result, m.DistanceToJackMm);

        int red, blue;
        if (request.RedPointsOverride is not null || request.BluePointsOverride is not null)
        {
            if (request.RedPointsOverride is null || request.BluePointsOverride is null)
                throw new GameRuleException("Para un puntaje manual hay que indicar los puntos de ambos lados.");
            if (request.RedPointsOverride > 0 && request.BluePointsOverride > 0)
                throw new GameRuleException("En un parcial solo puntúa un lado, salvo empate de distancias; revisá el puntaje manual.");
            (red, blue) = (request.RedPointsOverride.Value, request.BluePointsOverride.Value);
        }
        else
        {
            if (!end.Throws.Any(t => t.BallType == BallType.Jack))
                throw new GameRuleException("No se lanzó el jack: no se puede calcular el puntaje del parcial.");

            var inPlay = end.Throws
                .Where(t => t.BallType != BallType.Jack && t.Result == ThrowResult.InPlay)
                .ToList();

            var unmeasured = inPlay.Where(t => t.DistanceToJackMm is null).Select(t => t.Sequence).ToList();
            if (unmeasured.Count > 0)
                throw new GameRuleException($"Falta medir la distancia al jack de los lanzamientos: {string.Join(", ", unmeasured)}.");

            (red, blue) = EndScoring.Calculate(inPlay.Select(t => new EndScoring.ScoredBall(t.Side, t.DistanceToJackMm!.Value)));
        }

        end.RedPoints = (byte)red;
        end.BluePoints = (byte)blue;
        end.Status = EndStatus.Finished;
        end.FinishedAt = Now;

        AdvanceMatch(match, end);

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    /// <summary>Abre un parcial de desempate cuando el partido terminó empatado.</summary>
    public async Task<MatchStateDto> StartTieBreakAsync(int matchId, StartTieBreakRequest request, CancellationToken ct = default)
    {
        var match = await LoadAsync(matchId, ct);
        if (!match.IsAwaitingTieBreak())
            throw new GameRuleException("El partido no está esperando un desempate.");

        OpenEnd(match, NextEndNumber(match), request.JackSide!.Value, isTieBreak: true);

        await db.SaveChangesAsync(ct);
        return await PublishAsync(matchId, ct);
    }

    // ---------------------------------------------------------------- Auxiliares

    private void AdvanceMatch(Match match, End finished)
    {
        var regularEnds = match.FinishedRegularEnds();
        var numberOfEnds = match.Division.NumberOfEnds;

        if (regularEnds < numberOfEnds)
        {
            // El jack alterna entre lados: rojo en los parciales impares, azul en los pares.
            var nextRegular = regularEnds + 1;
            OpenEnd(match, NextEndNumber(match), nextRegular % 2 == 1 ? Side.Red : Side.Blue, isTieBreak: false);
            return;
        }

        var (red, blue) = match.RegularScore();
        if (red != blue)
        {
            FinishMatch(match, red > blue ? Side.Red : Side.Blue);
        }
        else if (finished.IsTieBreak && finished.RedPoints != finished.BluePoints)
        {
            FinishMatch(match, finished.RedPoints > finished.BluePoints ? Side.Red : Side.Blue);
        }
        // Si sigue empatado, el partido queda esperando un desempate (StartTieBreakAsync).
    }

    private void FinishMatch(Match match, Side winner)
    {
        match.Status = MatchStatus.Finished;
        match.WinnerSide = winner;
        match.FinishedAt = Now;
    }

    private void OpenEnd(Match match, int endNumber, Side jackSide, bool isTieBreak) =>
        match.Ends.Add(new End
        {
            EndNumber = (byte)endNumber,
            JackSide = jackSide,
            IsTieBreak = isTieBreak,
            Status = EndStatus.InProgress,
            StartedAt = Now,
        });

    private static int NextEndNumber(Match match) =>
        match.Ends.Count == 0 ? 1 : match.Ends.Max(e => e.EndNumber) + 1;

    private static End RequireCurrentEnd(Match match)
    {
        if (match.Status != MatchStatus.InProgress)
            throw new GameRuleException("El partido no está en juego.");

        return match.CurrentEnd()
            ?? throw new GameRuleException("No hay un parcial en juego. Si el partido está empatado, iniciá el desempate.");
    }

    private static Throw FindThrow(End end, int throwId) =>
        end.Throws.SingleOrDefault(t => t.ThrowId == throwId)
        ?? throw new NotFoundException($"El lanzamiento {throwId} no pertenece al parcial en juego.");

    private static void ApplyMeasurement(Throw thrown, ThrowResult result, int? distanceMm)
    {
        if (thrown.BallType == BallType.Jack && distanceMm is not null)
            throw new GameRuleException("El jack no tiene distancia al jack.");

        thrown.Result = result;
        thrown.DistanceToJackMm = result == ThrowResult.InPlay ? distanceMm : null;
    }

    private static void EnsureAthleteInSide(Match match, Side side, int athleteId)
    {
        if (match.SideOf(side).Members.All(m => m.AthleteId != athleteId))
            throw new GameRuleException($"El deportista {athleteId} no pertenece al lado {side.ToSpanish()}.");
    }

    private static MatchSide BuildSide(Side side, CreateSideRequest request, Division division, Dictionary<int, Athlete> athletes)
    {
        if (request.AthleteIds.Count > 0 && request.AthleteIds.Count != division.PlayersPerSide)
            throw new GameRuleException(
                $"La división {division.Code} requiere {division.PlayersPerSide} titular(es) por lado; el lado {side.ToSpanish()} tiene {request.AthleteIds.Count}.");

        var members = request.AthleteIds
            .Select((id, i) => new MatchSideMember { Athlete = athletes[id], PlayerNumber = (byte)(i + 1) })
            .Concat(request.SubstituteIds.Select((id, i) => new MatchSideMember
            {
                Athlete = athletes[id],
                PlayerNumber = (byte)(request.AthleteIds.Count + i + 1),
                IsSubstitute = true,
            }))
            .ToList();

        return new MatchSide { Side = side, DisplayName = request.DisplayName.Trim(), Members = members };
    }

    private async Task<Match> LoadAsync(int matchId, CancellationToken ct) =>
        await LoadQuery().SingleOrDefaultAsync(m => m.MatchId == matchId, ct)
            ?? throw new NotFoundException($"No existe el partido {matchId}.");

    private IQueryable<Match> LoadQuery() =>
        db.Matches
            .Include(m => m.Division)
            .Include(m => m.Sides).ThenInclude(s => s.Members).ThenInclude(m => m.Athlete)
            .Include(m => m.Ends).ThenInclude(e => e.Throws)
            .Include(m => m.Ends).ThenInclude(e => e.Penalties)
            .AsSplitQuery();

    /// <summary>Relee el partido desde la base (con los IDs generados) y lo publica por SignalR.</summary>
    private async Task<MatchStateDto> PublishAsync(int matchId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var match = await LoadQuery().AsNoTracking().SingleAsync(m => m.MatchId == matchId, ct);
        var state = match.ToStateDto();
        await notifier.MatchChangedAsync(state, match.ToSummaryDto(), ct);
        return state;
    }
}
