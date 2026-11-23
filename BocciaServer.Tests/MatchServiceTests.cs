using BocciaServer.Data;
using BocciaServer.Services;
using BocciaShared.Contracts;
using BocciaShared.Domain;

namespace BocciaServer.Tests;

public class MatchServiceTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private MatchService Service => _ctx.Service;

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Iniciar_abre_el_primer_parcial_con_jack_rojo()
    {
        var state = await _ctx.CreateStartedMatchAsync();

        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.Equal(1, state.CurrentEnd!.EndNumber);
        Assert.Equal(Side.Red, state.CurrentEnd.JackSide);
        Assert.Equal(6, state.Red.ColourBallsRemaining);
        Assert.Equal(6, state.Blue.ColourBallsRemaining);
    }

    [Fact]
    public async Task No_se_puede_lanzar_bola_de_color_antes_del_jack()
    {
        var state = await _ctx.CreateStartedMatchAsync();

        var ex = await Assert.ThrowsAsync<GameRuleException>(() => _ctx.ThrowAsync(state.MatchId, Side.Red));
        Assert.Contains("jack", ex.Message);
    }

    [Fact]
    public async Task El_jack_lo_lanza_el_lado_que_corresponde()
    {
        var state = await _ctx.CreateStartedMatchAsync();

        await Assert.ThrowsAsync<GameRuleException>(() => _ctx.ThrowAsync(state.MatchId, Side.Blue, BallType.Jack));
    }

    [Fact]
    public async Task Un_lado_no_puede_lanzar_mas_de_seis_bolas()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);
        for (var i = 0; i < 6; i++) await _ctx.ThrowAsync(id, Side.Red);

        var state = await Service.GetStateAsync(id);
        Assert.Equal(0, state.Red.ColourBallsRemaining);
        await Assert.ThrowsAsync<GameRuleException>(() => _ctx.ThrowAsync(id, Side.Red));
    }

    [Fact]
    public async Task Partido_completo_gana_el_lado_con_mas_puntos_y_el_jack_alterna()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;

        await _ctx.PlayEndAsync(id, red: [10, 20, 300], blue: [100, 200]);  // rojo 2
        await _ctx.PlayEndAsync(id, red: [150], blue: [50, 400]);           // azul 1
        await _ctx.PlayEndAsync(id, red: [30], blue: [60]);                  // rojo 1
        var state = await _ctx.PlayEndAsync(id, red: [80], blue: [90]);     // rojo 1

        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal(Side.Red, state.WinnerSide);
        Assert.Equal(4, state.Red.Score);
        Assert.Equal(1, state.Blue.Score);
        Assert.Null(state.CurrentEnd);
        Assert.Equal([1, 2, 3, 4], state.Ends.Select(e => e.EndNumber));

        var jackSides = _ctx.Db.Ends.Where(e => e.MatchId == id).OrderBy(e => e.EndNumber).Select(e => e.JackSide).ToList();
        Assert.Equal([Side.Red, Side.Blue, Side.Red, Side.Blue], jackSides);
    }

    [Fact]
    public async Task Empate_espera_desempate_y_el_desempate_define_sin_sumar_al_marcador()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.PlayEndAsync(id, red: [10], blue: [20]);   // rojo 1
        await _ctx.PlayEndAsync(id, red: [20], blue: [10]);   // azul 1
        await _ctx.PlayEndAsync(id, red: [10], blue: [10]);   // 1 - 1
        var state = await _ctx.PlayEndAsync(id, red: [50], blue: [50]); // 1 - 1

        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.True(state.AwaitingTieBreak);
        Assert.Equal(state.Red.Score, state.Blue.Score);
        await Assert.ThrowsAsync<GameRuleException>(() => _ctx.ThrowAsync(id, Side.Red, BallType.Jack));

        state = await Service.StartTieBreakAsync(id, new StartTieBreakRequest { JackSide = Side.Blue });
        Assert.True(state.CurrentEnd!.IsTieBreak);
        Assert.Equal(Side.Blue, state.CurrentEnd.JackSide);

        state = await _ctx.PlayEndAsync(id, red: [5], blue: [40]);

        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal(Side.Red, state.WinnerSide);
        Assert.Equal(3, state.Red.Score);
        Assert.Equal(3, state.Blue.Score);
        Assert.Equal(1, state.Red.TieBreakScore);
    }

    [Fact]
    public async Task Desempate_empatado_requiere_otro_desempate()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        for (var i = 0; i < 4; i++) await _ctx.PlayEndAsync(id, red: [10], blue: [10]);
        await Service.StartTieBreakAsync(id, new StartTieBreakRequest { JackSide = Side.Red });

        var state = await _ctx.PlayEndAsync(id, red: [30], blue: [30]);

        Assert.True(state.AwaitingTieBreak);
        Assert.Equal(MatchStatus.InProgress, state.Status);
    }

    [Fact]
    public async Task No_se_puede_cerrar_el_parcial_con_bolas_sin_medir()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);
        await _ctx.ThrowAsync(id, Side.Red);
        await _ctx.ThrowAsync(id, Side.Blue, result: ThrowResult.Dead);

        var ex = await Assert.ThrowsAsync<GameRuleException>(() => Service.FinishEndAsync(id, new FinishEndRequest()));
        Assert.Contains("2", ex.Message); // secuencia de la bola roja sin medir
    }

    [Fact]
    public async Task Las_bolas_muertas_no_puntuan()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);
        var state = await _ctx.ThrowAsync(id, Side.Red);
        state = await _ctx.ThrowAsync(id, Side.Blue);
        var redBall = state.CurrentEnd!.Throws.Single(t => t.Side == Side.Red && t.BallType == BallType.Colour);
        var blueBall = state.CurrentEnd.Throws.Single(t => t.Side == Side.Blue);

        state = await Service.FinishEndAsync(id, new FinishEndRequest
        {
            Measurements =
            [
                new ThrowMeasurement { ThrowId = redBall.ThrowId, Result = ThrowResult.Dead },
                new ThrowMeasurement { ThrowId = blueBall.ThrowId, DistanceToJackMm = 900 },
            ],
        });

        Assert.Equal((0, 1), (state.Ends[0].RedPoints, state.Ends[0].BluePoints));
    }

    [Fact]
    public async Task Bola_de_penalizacion_requiere_sancion_al_rival_y_puntua()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);

        await Assert.ThrowsAsync<GameRuleException>(() => _ctx.ThrowAsync(id, Side.Red, BallType.Penalty));

        var state = await Service.AddPenaltyAsync(id, new AddPenaltyRequest { PenalizedSide = Side.Blue, PenaltyType = PenaltyType.PenaltyBall });
        Assert.Equal(1, state.Red.PenaltyBallsAvailable);
        Assert.Equal(0, state.Blue.PenaltyBallsAvailable);

        await _ctx.ThrowAsync(id, Side.Blue);
        state = await _ctx.ThrowAsync(id, Side.Red, BallType.Penalty);
        Assert.Equal(0, state.Red.PenaltyBallsAvailable);
        Assert.Equal(6, state.Red.ColourBallsRemaining); // la bola de penalización no consume bolas de color

        var blue = state.CurrentEnd!.Throws.Single(t => t.Side == Side.Blue);
        var penalty = state.CurrentEnd.Throws.Single(t => t.BallType == BallType.Penalty);
        state = await Service.FinishEndAsync(id, new FinishEndRequest
        {
            Measurements =
            [
                new ThrowMeasurement { ThrowId = blue.ThrowId, DistanceToJackMm = 100 },
                new ThrowMeasurement { ThrowId = penalty.ThrowId, DistanceToJackMm = 20 },
            ],
        });
        Assert.Equal(1, state.Ends[0].RedPoints);
    }

    [Fact]
    public async Task Deshacer_elimina_el_ultimo_lanzamiento()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);
        await _ctx.ThrowAsync(id, Side.Red);

        var state = await Service.UndoLastThrowAsync(id);

        Assert.Single(state.CurrentEnd!.Throws);
        Assert.Equal(6, state.Red.ColourBallsRemaining);
    }

    [Fact]
    public async Task Puntaje_manual_reemplaza_el_calculo()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;

        var state = await Service.FinishEndAsync(id, new FinishEndRequest { RedPointsOverride = 0, BluePointsOverride = 3 });

        Assert.Equal(3, state.Blue.Score);
        Assert.Equal(2, state.CurrentEnd!.EndNumber);
    }

    [Fact]
    public async Task Crear_partido_valida_la_cantidad_de_titulares()
    {
        var a = new Athlete { FirstName = "Ana", LastName = "Pérez" };
        var b = new Athlete { FirstName = "Luis", LastName = "Gómez" };
        _ctx.Db.Athletes.AddRange(a, b);
        await _ctx.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<GameRuleException>(() => Service.CreateAsync(new CreateMatchRequest
        {
            DivisionCode = "PAIRS_BC3",
            Red = new CreateSideRequest { DisplayName = "Rojo", AthleteIds = [a.AthleteId] },
            Blue = new CreateSideRequest { DisplayName = "Azul" },
        }));
        Assert.Contains("2 titular", ex.Message);
    }

    [Fact]
    public async Task Parejas_limita_las_bolas_por_deportista()
    {
        var athletes = Enumerable.Range(1, 4).Select(i => new Athlete { FirstName = $"J{i}", LastName = "X" }).ToList();
        _ctx.Db.Athletes.AddRange(athletes);
        await _ctx.Db.SaveChangesAsync();

        var state = await Service.CreateAsync(new CreateMatchRequest
        {
            DivisionCode = "PAIRS_BC3",
            Red = new CreateSideRequest { DisplayName = "Rojo", AthleteIds = [athletes[0].AthleteId, athletes[1].AthleteId] },
            Blue = new CreateSideRequest { DisplayName = "Azul", AthleteIds = [athletes[2].AthleteId, athletes[3].AthleteId] },
        });
        var id = (await Service.StartAsync(state.MatchId)).MatchId;
        var player = athletes[0].AthleteId;

        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);
        for (var i = 0; i < 3; i++)
            await Service.RegisterThrowAsync(id, new RegisterThrowRequest { Side = Side.Red, AthleteId = player });

        await Assert.ThrowsAsync<GameRuleException>(() =>
            Service.RegisterThrowAsync(id, new RegisterThrowRequest { Side = Side.Red, AthleteId = player }));
        await Assert.ThrowsAsync<GameRuleException>(() =>
            Service.RegisterThrowAsync(id, new RegisterThrowRequest { Side = Side.Red, AthleteId = athletes[2].AthleteId }));
    }

    [Fact]
    public async Task Cada_cambio_se_notifica_a_los_clientes()
    {
        var id = (await _ctx.CreateStartedMatchAsync()).MatchId;
        await _ctx.ThrowAsync(id, Side.Red, BallType.Jack);

        Assert.Equal(3, _ctx.Notifier.States.Count); // crear, iniciar, jack
        Assert.True(_ctx.Notifier.States[^1].CurrentEnd!.JackThrown);
        Assert.Equal(MatchStatus.InProgress, _ctx.Notifier.Summaries[^1].Status);
    }

    [Fact]
    public async Task Partido_inexistente_lanza_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetStateAsync(999));
}
