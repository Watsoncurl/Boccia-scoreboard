using BocciaShared.Domain;
using Microsoft.EntityFrameworkCore;

namespace BocciaServer.Data;

/// <summary>
/// Base de datos SQLite para desarrollo y pruebas locales sin SQL Server.
/// Crea las tablas desde el modelo de EF Core y carga las divisiones oficiales
/// (las mismas que Script.PostDeployment.sql del proyecto BocciaDatabase).
/// En producción se usa SQL Server con el esquema del proyecto BocciaDatabase.
/// </summary>
public static class DevelopmentDatabase
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BocciaDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Divisions.AnyAsync()) return;

        db.Divisions.AddRange(
            Division("BC1", "Individual BC1", MatchFormat.Individual, 1, 6, 4),
            Division("BC2", "Individual BC2", MatchFormat.Individual, 1, 6, 4),
            Division("BC3", "Individual BC3", MatchFormat.Individual, 1, 6, 4),
            Division("BC4", "Individual BC4", MatchFormat.Individual, 1, 6, 4),
            Division("PAIRS_BC3", "Parejas BC3", MatchFormat.Pairs, 2, 3, 4),
            Division("PAIRS_BC4", "Parejas BC4", MatchFormat.Pairs, 2, 3, 4),
            Division("TEAM_BC1BC2", "Equipos BC1/BC2", MatchFormat.Team, 3, 2, 6));
        await db.SaveChangesAsync();
    }

    private static Division Division(string code, string name, MatchFormat type, byte players, byte balls, byte ends) =>
        new() { Code = code, Name = name, MatchType = type, PlayersPerSide = players, BallsPerPlayer = balls, NumberOfEnds = ends };
}
