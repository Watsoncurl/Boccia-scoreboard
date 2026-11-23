using BocciaShared.Contracts;

namespace BocciaServer.Services;

/// <summary>Publica los cambios de un partido a los clientes conectados.</summary>
public interface IMatchNotifier
{
    Task MatchChangedAsync(MatchStateDto state, MatchSummaryDto summary, CancellationToken ct = default);
}
