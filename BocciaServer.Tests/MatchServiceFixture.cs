using BocciaServer.Data;
using BocciaServer.Services;
using BocciaShared.Contracts;
using BocciaShared.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BocciaServer.Tests;

/// <summary>
/// Base de datos SQLite en memoria con el modelo de EF Core y las divisiones
/// oficiales cargadas. Cada prueba crea su propia instancia.
/// </summary>
public sealed class TestContext : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public BocciaDbContext Db { get; }
    public RecordingNotifier Notifier { get; } = new();
    public MatchService Service { get; }

    public TestContext()
    {
        _connection.Open();
        Db = new BocciaDbContext(new DbContextOptionsBuilder<BocciaDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();

        Db.Divisions.AddRange(
            new Division { Code = "BC2", Name = "Individual BC2", MatchType = MatchFormat.Individual, PlayersPerSide = 1, BallsPerPlayer = 6, NumberOfEnds = 4 },
            new Division { Code = "PAIRS_BC3", Name = "Parejas BC3", MatchType = MatchFormat.Pairs, PlayersPerSide = 2, BallsPerPlayer = 3, NumberOfEnds = 4 },
            new Division { Code = "TEAM_BC1BC2", Name = "Equipos BC1/BC2", MatchType = MatchFormat.Team, PlayersPerSide = 3, BallsPerPlayer = 2, NumberOfEnds = 6 });
        Db.SaveChanges();

        Service = new MatchService(Db, Notifier, TimeProvider.System);
    }

    public Task<MatchStateDto> CreateStartedMatchAsync(string division = "BC2") =>
        CreateMatchAsync(division).ContinueWith(t => Service.StartAsync(t.Result.MatchId)).Unwrap();

    public Task<MatchStateDto> CreateMatchAsync(string division = "BC2") =>
        Service.CreateAsync(new CreateMatchRequest
        {
            DivisionCode = division,
            Red = new CreateSideRequest { DisplayName = "Rojo" },
            Blue = new CreateSideRequest { DisplayName = "Azul" },
        });

    public Task<MatchStateDto> ThrowAsync(int matchId, Side side, BallType type = BallType.Colour, ThrowResult result = ThrowResult.InPlay) =>
        Service.RegisterThrowAsync(matchId, new RegisterThrowRequest { Side = side, BallType = type, Result = result });

    /// <summary>
    /// Juega un parcial completo: lanza el jack, las bolas indicadas (distancias en mm)
    /// y cierra el parcial con esas mediciones.
    /// </summary>
    public async Task<MatchStateDto> PlayEndAsync(int matchId, int[] red, int[] blue)
    {
        var state = await Service.GetStateAsync(matchId);
        await ThrowAsync(matchId, state.CurrentEnd!.JackSide, BallType.Jack);
        foreach (var _ in red) await ThrowAsync(matchId, Side.Red);
        foreach (var _ in blue) await ThrowAsync(matchId, Side.Blue);

        state = await Service.GetStateAsync(matchId);
        var redThrows = state.CurrentEnd!.Throws.Where(t => t.Side == Side.Red && t.BallType == BallType.Colour).ToList();
        var blueThrows = state.CurrentEnd!.Throws.Where(t => t.Side == Side.Blue && t.BallType == BallType.Colour).ToList();

        var measurements = redThrows.Zip(red, (t, d) => new ThrowMeasurement { ThrowId = t.ThrowId, DistanceToJackMm = d })
            .Concat(blueThrows.Zip(blue, (t, d) => new ThrowMeasurement { ThrowId = t.ThrowId, DistanceToJackMm = d }))
            .ToList();

        return await Service.FinishEndAsync(matchId, new FinishEndRequest { Measurements = measurements });
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

public class RecordingNotifier : IMatchNotifier
{
    public List<MatchStateDto> States { get; } = [];
    public List<MatchSummaryDto> Summaries { get; } = [];

    public Task MatchChangedAsync(MatchStateDto state, MatchSummaryDto summary, CancellationToken ct = default)
    {
        States.Add(state);
        Summaries.Add(summary);
        return Task.CompletedTask;
    }
}
