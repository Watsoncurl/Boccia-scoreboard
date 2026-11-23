using BocciaShared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BocciaServer.Hubs;

/// <summary>
/// Hub de SignalR del marcador. Los clientes (control y marcador) se suscriben
/// a un partido o al listado general; los cambios los envía <see cref="SignalRMatchNotifier"/>.
/// </summary>
public class ScoreboardHub : Hub<IScoreboardClient>
{
    public const string LobbyGroup = "lobby";

    public static string MatchGroup(int matchId) => $"match-{matchId}";

    public Task JoinMatch(int matchId) => Groups.AddToGroupAsync(Context.ConnectionId, MatchGroup(matchId));

    public Task LeaveMatch(int matchId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, MatchGroup(matchId));

    public Task JoinLobby() => Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup);

    public Task LeaveLobby() => Groups.RemoveFromGroupAsync(Context.ConnectionId, LobbyGroup);
}
