using System.Net.WebSockets;
using System.Text;
using BattleHunter.Core.Serialization;

namespace BattleHunter.Server.Net;

/// <summary>Uma conexão WebSocket de um jogador: envio serializado (um frame por vez) e recepção de mensagens JSON.</summary>
public sealed class Connection : IAsyncDisposable
{
    private readonly WebSocket _socket;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ILogger _log;

    public Connection(WebSocket socket, ILogger log)
    {
        _socket = socket;
        _log = log;
    }

    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public bool IsOpen => _socket.State == WebSocketState.Open;

    public async Task SendAsync(ServerMessage message, CancellationToken ct = default)
    {
        if (!IsOpen)
            return;

        var bytes = Encoding.UTF8.GetBytes(MessageJson.Serialize(message));
        await _sendLock.WaitAsync(ct);
        try
        {
            if (IsOpen)
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or OperationCanceledException)
        {
            _log.LogDebug(ex, "Envio falhou na conexão {Connection}", Id);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>Lê a próxima mensagem; null quando a conexão fecha.</summary>
    public async Task<ClientMessage?> ReceiveAsync(CancellationToken ct)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();

        while (true)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await _socket.ReceiveAsync(buffer, ct);
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
            {
                return null;
            }

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            stream.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
                continue;

            var json = Encoding.UTF8.GetString(stream.ToArray());
            stream.SetLength(0);

            try
            {
                return MessageJson.Deserialize<ClientMessage>(json);
            }
            catch (Exception ex)
            {
                _log.LogWarning("Mensagem inválida na conexão {Connection}: {Error}", Id, ex.Message);
                await SendAsync(new Error("Mensagem inválida: " + ex.Message), ct);
            }
        }
    }

    public async Task CloseAsync(string reason)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _socket.Dispose();
        _sendLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
