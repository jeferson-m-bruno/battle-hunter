using System.Net.WebSockets;
using System.Text;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using Microsoft.AspNetCore.TestHost;

namespace BattleHunter.Server.Tests.Support;

/// <summary>Cliente WebSocket de teste: fala o protocolo e guarda tudo que recebeu.</summary>
public sealed class TestClient : IAsyncDisposable
{
    private readonly WebSocket _socket;
    private readonly List<ServerMessage> _received = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _reader;

    private TestClient(WebSocket socket, string deviceId)
    {
        _socket = socket;
        DeviceId = deviceId;
        _reader = Task.Run(ReadLoop);
    }

    public string DeviceId { get; }
    public string? PlayerId { get; private set; }
    public int HunterId { get; private set; }
    public PlayerSnapshot? Snapshot { get; private set; }
    public IReadOnlyList<ServerMessage> Received { get { lock (_received) return _received.ToList(); } }

    public static async Task<TestClient> ConnectAsync(TestServer server, string deviceId, string name)
    {
        var ws = await server.CreateWebSocketClient().ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        var client = new TestClient(ws, deviceId);
        await client.SendAsync(new Auth(deviceId, name));
        var welcome = await client.WaitForAsync<Welcome>();
        client.PlayerId = welcome.PlayerId;
        return client;
    }

    public async Task SendAsync(ClientMessage message)
    {
        var bytes = Encoding.UTF8.GetBytes(MessageJson.Serialize(message));
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public Task ActAsync(GameAction action) => SendAsync(new PlayerAction(action));

    public async Task<T> WaitForAsync<T>(Func<T, bool>? predicate = null, int timeoutMs = 20_000) where T : ServerMessage
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var seen = 0;
        while (DateTime.UtcNow < deadline)
        {
            List<ServerMessage> snapshot;
            lock (_received)
                snapshot = _received.Skip(seen).ToList();
            foreach (var m in snapshot)
            {
                seen++;
                if (m is T t && (predicate == null || predicate(t)))
                    return t;
            }

            await _signal.WaitAsync(Math.Max(1, (int)(deadline - DateTime.UtcNow).TotalMilliseconds));
        }

        throw new TimeoutException($"{typeof(T).Name} não chegou em {timeoutMs} ms. Recebidos: {string.Join(", ", Received.Select(m => m.GetType().Name))}");
    }

    /// <summary>Joga passando: na vez do seu caçador, rola e passa até a partida acabar.</summary>
    public async Task PlayPassingUntilOverAsync(int timeoutMs = 60_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var acted = -1;
        while (DateTime.UtcNow < deadline)
        {
            if (Received.OfType<GameOver>().Any())
                return;

            var s = Snapshot;
            if (s != null && s.Phase != GamePhase.Finished && s.CurrentHunterId == HunterId)
            {
                var key = s.Round * 100 + (s.Phase == GamePhase.AwaitingRoll ? 0 : 1);
                if (key != acted)
                {
                    acted = key;
                    await ActAsync(s.Phase == GamePhase.AwaitingRoll ? new RollDice(HunterId) : new Pass(HunterId));
                }
            }

            await _signal.WaitAsync(200);
        }

        throw new TimeoutException("A partida não terminou a tempo.");
    }

    public async Task CloseAsync()
    {
        _cts.Cancel();
        try
        {
            if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or InvalidOperationException or OperationCanceledException)
        {
            // O servidor pode já ter fechado o socket (fim de partida): fechar de novo não é erro.
        }
    }

    private async Task ReadLoop()
    {
        var buffer = new byte[64 * 1024];
        using var stream = new MemoryStream();
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                stream.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                    continue;

                var message = MessageJson.Deserialize<ServerMessage>(Encoding.UTF8.GetString(stream.ToArray()));
                stream.SetLength(0);

                switch (message)
                {
                    case MatchStarted ms:
                        HunterId = ms.HunterId;
                        Snapshot = ms.Snapshot;
                        break;
                    case MatchResumed mr:
                        HunterId = mr.HunterId;
                        Snapshot = mr.Snapshot;
                        break;
                    case MatchUpdate mu:
                        Snapshot = mu.Snapshot;
                        break;
                    case GameOver go:
                        Snapshot = go.Snapshot;
                        break;
                }

                lock (_received)
                    _received.Add(message);
                _signal.Release();
            }
        }
        catch (Exception) when (_cts.IsCancellationRequested || _socket.State != WebSocketState.Open)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _socket.Dispose();
    }
}
