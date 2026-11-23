using System.Text.Json.Serialization;
using BocciaShared.Contracts;
using BocciaShared.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BocciaClient;

/// <summary>
/// Conexión SignalR al hub del marcador (una por circuito de Blazor).
/// Se reconecta sola, también si el servidor no estaba disponible al inicio,
/// y vuelve a suscribirse a los partidos y al listado tras cada reconexión.
/// </summary>
public sealed class ScoreboardConnection : IAsyncDisposable
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private readonly HubConnection _hub;
    private readonly ILogger<ScoreboardConnection> _logger;
    private readonly HashSet<int> _matches = [];
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly CancellationTokenSource _disposed = new();
    private bool _lobby;
    private Task? _connectLoop;

    /// <summary>Estado completo de un partido al que se está suscripto.</summary>
    public event Func<MatchStateDto, Task>? MatchUpdated;

    /// <summary>Resumen de cualquier partido creado o modificado (requiere JoinLobby).</summary>
    public event Func<MatchSummaryDto, Task>? MatchListChanged;

    /// <summary>
    /// Cambió el estado de la conexión. Tras reconectar, los componentes deberían
    /// recargar los datos porque pudieron perderse notificaciones.
    /// </summary>
    public event Func<bool, Task>? ConnectionChanged;

    public bool IsConnected => _hub.State == HubConnectionState.Connected;

    public ScoreboardConnection(IOptions<BocciaServerOptions> options, ILogger<ScoreboardConnection> logger)
    {
        _logger = logger;
        var hubUrl = new Uri(new Uri(options.Value.BaseUrl), ScoreboardHubContract.Path);

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .WithAutomaticReconnect(new RetryForeverPolicy())
            .Build();

        _hub.On<MatchStateDto>(ScoreboardHubContract.MatchUpdated, state => RaiseAsync(MatchUpdated, state));
        _hub.On<MatchSummaryDto>(ScoreboardHubContract.MatchListChanged, summary => RaiseAsync(MatchListChanged, summary));

        _hub.Reconnecting += _ => RaiseAsync(ConnectionChanged, false);
        _hub.Reconnected += async _ =>
        {
            await RejoinAsync();
            await RaiseAsync(ConnectionChanged, true);
        };
        _hub.Closed += async _ =>
        {
            await RaiseAsync(ConnectionChanged, false);
            StartConnectLoop();
        };
    }

    /// <summary>Inicia la conexión (en segundo plano, con reintentos) si todavía no está iniciada.</summary>
    public void EnsureStarted() => StartConnectLoop();

    public Task JoinMatchAsync(int matchId) => JoinAsync(() => _matches.Add(matchId), ScoreboardHubContract.JoinMatch, matchId);

    public async Task LeaveMatchAsync(int matchId)
    {
        _matches.Remove(matchId);
        if (IsConnected) await _hub.InvokeAsync(ScoreboardHubContract.LeaveMatch, matchId);
    }

    public Task JoinLobbyAsync() => JoinAsync(() => _lobby = true, ScoreboardHubContract.JoinLobby, null);

    private async Task JoinAsync(Action remember, string method, int? matchId)
    {
        remember();
        if (IsConnected)
        {
            await InvokeJoinAsync(method, matchId);
            return;
        }
        // Si todavía no hay conexión, la suscripción se hace al conectar.
        StartConnectLoop();
    }

    private void StartConnectLoop()
    {
        if (_disposed.IsCancellationRequested || _connectLoop is { IsCompleted: false }) return;
        _connectLoop = Task.Run(ConnectLoopAsync);
    }

    private async Task ConnectLoopAsync()
    {
        while (!_disposed.IsCancellationRequested)
        {
            await _startLock.WaitAsync(_disposed.Token);
            try
            {
                if (_hub.State == HubConnectionState.Disconnected)
                {
                    await _hub.StartAsync(_disposed.Token);
                    await RejoinAsync();
                    await RaiseAsync(ConnectionChanged, true);
                }
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("No se pudo conectar con el hub del servidor: {Message}. Reintentando...", ex.Message);
            }
            finally
            {
                _startLock.Release();
            }

            await Task.Delay(RetryDelay, _disposed.Token);
        }
    }

    private async Task RejoinAsync()
    {
        foreach (var matchId in _matches.ToList()) await InvokeJoinAsync(ScoreboardHubContract.JoinMatch, matchId);
        if (_lobby) await InvokeJoinAsync(ScoreboardHubContract.JoinLobby, null);
    }

    private Task InvokeJoinAsync(string method, int? matchId) =>
        matchId is null ? _hub.InvokeAsync(method) : _hub.InvokeAsync(method, matchId.Value);

    private async Task RaiseAsync<T>(Func<T, Task>? handlers, T arg)
    {
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Func<T, Task>>())
        {
            try { await handler(arg); }
            catch (Exception ex) { _logger.LogError(ex, "Error en un suscriptor del hub."); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed.Cancel();
        try { await _hub.DisposeAsync(); }
        catch (Exception ex) { _logger.LogDebug(ex, "Error al cerrar la conexión del hub."); }
        _disposed.Dispose();
    }

    private sealed class RetryForeverPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext context) =>
            context.PreviousRetryCount == 0 ? TimeSpan.Zero : RetryDelay;
    }
}
