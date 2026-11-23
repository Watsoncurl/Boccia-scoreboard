using BocciaShared.Contracts;

namespace BocciaShared.Hubs;

/// <summary>
/// Contrato del hub de SignalR del marcador.
/// Los clientes se conectan a <see cref="Path"/> y se suscriben con
/// JoinMatch(matchId) para recibir los cambios de un partido, o con
/// JoinLobby() para recibir los cambios de todos los partidos.
/// Los enums viajan como texto ("Red", "InProgress", ...).
/// </summary>
public static class ScoreboardHubContract
{
    public const string Path = "/hubs/scoreboard";

    // Métodos del servidor que invocan los clientes.
    public const string JoinMatch = "JoinMatch";
    public const string LeaveMatch = "LeaveMatch";
    public const string JoinLobby = "JoinLobby";
    public const string LeaveLobby = "LeaveLobby";

    // Eventos que el servidor envía a los clientes.
    public const string MatchUpdated = nameof(IScoreboardClient.MatchUpdated);
    public const string MatchListChanged = nameof(IScoreboardClient.MatchListChanged);
}

/// <summary>Eventos que el servidor envía a los clientes conectados.</summary>
public interface IScoreboardClient
{
    /// <summary>Estado completo de un partido tras cualquier cambio.</summary>
    Task MatchUpdated(MatchStateDto state);

    /// <summary>Resumen de un partido creado o modificado (para listados).</summary>
    Task MatchListChanged(MatchSummaryDto summary);
}
