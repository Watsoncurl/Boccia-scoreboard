using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BocciaShared.Contracts;
using BocciaShared.Domain;

namespace BocciaClient;

/// <summary>Cliente tipado de la API REST de BocciaServer.</summary>
public class BocciaApiClient(HttpClient http)
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Catálogos
    public Task<List<DivisionDto>> GetDivisionsAsync(CancellationToken ct = default) =>
        SendAsync<List<DivisionDto>>(HttpMethod.Get, "api/divisions", null, ct);

    public Task<List<AthleteDto>> GetAthletesAsync(CancellationToken ct = default) =>
        SendAsync<List<AthleteDto>>(HttpMethod.Get, "api/athletes", null, ct);

    public Task<AthleteDto> CreateAthleteAsync(CreateAthleteRequest request, CancellationToken ct = default) =>
        SendAsync<AthleteDto>(HttpMethod.Post, "api/athletes", request, ct);

    // Partidos
    public Task<List<MatchSummaryDto>> GetMatchesAsync(MatchStatus? status = null, CancellationToken ct = default) =>
        SendAsync<List<MatchSummaryDto>>(HttpMethod.Get, status is null ? "api/matches" : $"api/matches?status={status}", null, ct);

    public Task<MatchStateDto> GetMatchAsync(int matchId, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Get, $"api/matches/{matchId}", null, ct);

    public Task<MatchStateDto> CreateMatchAsync(CreateMatchRequest request, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, "api/matches", request, ct);

    public Task<MatchStateDto> StartMatchAsync(int matchId, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, $"api/matches/{matchId}/start", null, ct);

    public Task<MatchStateDto> CancelMatchAsync(int matchId, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, $"api/matches/{matchId}/cancel", null, ct);

    public Task<MatchStateDto> RegisterThrowAsync(int matchId, RegisterThrowRequest request, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, $"api/matches/{matchId}/throws", request, ct);

    public Task<MatchStateDto> UpdateThrowAsync(int matchId, int throwId, UpdateThrowRequest request, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Put, $"api/matches/{matchId}/throws/{throwId}", request, ct);

    public Task<MatchStateDto> UndoLastThrowAsync(int matchId, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Delete, $"api/matches/{matchId}/throws/last", null, ct);

    public Task<MatchStateDto> AddPenaltyAsync(int matchId, AddPenaltyRequest request, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, $"api/matches/{matchId}/penalties", request, ct);

    public Task<MatchStateDto> FinishEndAsync(int matchId, FinishEndRequest request, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, $"api/matches/{matchId}/ends/current/finish", request, ct);

    public Task<MatchStateDto> StartTieBreakAsync(int matchId, StartTieBreakRequest request, CancellationToken ct = default) =>
        SendAsync<MatchStateDto>(HttpMethod.Post, $"api/matches/{matchId}/tiebreak", request, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw await ApiException.FromResponseAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiException((int)response.StatusCode, "El servidor devolvió una respuesta vacía.");
    }
}
