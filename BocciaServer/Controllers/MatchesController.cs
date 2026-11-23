using BocciaServer.Services;
using BocciaShared.Contracts;
using BocciaShared.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BocciaServer.Controllers;

/// <summary>
/// API que usa la aplicación de control. Cada acción devuelve el estado
/// actualizado del partido y además lo publica por SignalR.
/// </summary>
[ApiController]
[Route("api/matches")]
public class MatchesController(MatchService matches) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<MatchSummaryDto>> List([FromQuery] MatchStatus? status, CancellationToken ct) =>
        matches.ListAsync(status, ct);

    [HttpGet("{matchId:int}")]
    public Task<MatchStateDto> Get(int matchId, CancellationToken ct) =>
        matches.GetStateAsync(matchId, ct);

    [HttpPost]
    public async Task<ActionResult<MatchStateDto>> Create(CreateMatchRequest request, CancellationToken ct)
    {
        var state = await matches.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { matchId = state.MatchId }, state);
    }

    [HttpPost("{matchId:int}/start")]
    public Task<MatchStateDto> Start(int matchId, CancellationToken ct) =>
        matches.StartAsync(matchId, ct);

    [HttpPost("{matchId:int}/cancel")]
    public Task<MatchStateDto> Cancel(int matchId, CancellationToken ct) =>
        matches.CancelAsync(matchId, ct);

    [HttpPost("{matchId:int}/throws")]
    public Task<MatchStateDto> RegisterThrow(int matchId, RegisterThrowRequest request, CancellationToken ct) =>
        matches.RegisterThrowAsync(matchId, request, ct);

    [HttpPut("{matchId:int}/throws/{throwId:int}")]
    public Task<MatchStateDto> UpdateThrow(int matchId, int throwId, UpdateThrowRequest request, CancellationToken ct) =>
        matches.UpdateThrowAsync(matchId, throwId, request, ct);

    [HttpDelete("{matchId:int}/throws/last")]
    public Task<MatchStateDto> UndoLastThrow(int matchId, CancellationToken ct) =>
        matches.UndoLastThrowAsync(matchId, ct);

    [HttpPost("{matchId:int}/penalties")]
    public Task<MatchStateDto> AddPenalty(int matchId, AddPenaltyRequest request, CancellationToken ct) =>
        matches.AddPenaltyAsync(matchId, request, ct);

    [HttpPost("{matchId:int}/ends/current/finish")]
    public Task<MatchStateDto> FinishEnd(int matchId, FinishEndRequest request, CancellationToken ct) =>
        matches.FinishEndAsync(matchId, request, ct);

    [HttpPost("{matchId:int}/tiebreak")]
    public Task<MatchStateDto> StartTieBreak(int matchId, StartTieBreakRequest request, CancellationToken ct) =>
        matches.StartTieBreakAsync(matchId, request, ct);
}
