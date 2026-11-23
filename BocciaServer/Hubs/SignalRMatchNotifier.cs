using BocciaServer.Services;
using BocciaShared.Contracts;
using BocciaShared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BocciaServer.Hubs;

public class SignalRMatchNotifier(IHubContext<ScoreboardHub, IScoreboardClient> hub) : IMatchNotifier
{
    public async Task MatchChangedAsync(MatchStateDto state, MatchSummaryDto summary, CancellationToken ct = default)
    {
        await hub.Clients.Group(ScoreboardHub.MatchGroup(state.MatchId)).MatchUpdated(state);
        await hub.Clients.Group(ScoreboardHub.LobbyGroup).MatchListChanged(summary);
    }
}
