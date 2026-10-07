using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleHunter.Core.Serialization;

namespace BattleHunter.Client.Net
{
    /// <summary>
    /// Cliente WebSocket do protocolo: envia ClientMessage, recebe ServerMessage numa thread de fundo
    /// e entrega na thread principal via Drain(). Sem lógica de jogo.
    /// </summary>
    public sealed class WebSocketClient : IDisposable
    {
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<ServerMessage> _inbox = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;
        public string LastError { get; private set; }

        public async Task<bool> ConnectAsync(string url, int timeoutMs = 8000)
        {
            Dispose();
            _socket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            try
            {
                using var timeout = new CancellationTokenSource(timeoutMs);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _cts.Token);
                await _socket.ConnectAsync(new Uri(url), linked.Token);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }

            _ = Task.Run(() => ReceiveLoop(_cts.Token));
            return true;
        }

        public async Task SendAsync(ClientMessage message)
        {
            if (!IsOpen)
                return;

            var bytes = Encoding.UTF8.GetBytes(MessageJson.Serialize(message));
            await _sendLock.WaitAsync();
            try
            {
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Send(ClientMessage message) => _ = SendAsync(message);

        /// <summary>Chamado na thread principal: devolve as mensagens chegadas desde a última vez.</summary>
        public bool TryDequeue(out ServerMessage message) => _inbox.TryDequeue(out message);

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[64 * 1024];
            using var stream = new System.IO.MemoryStream();
            try
            {
                while (!ct.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    stream.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage)
                        continue;

                    var json = Encoding.UTF8.GetString(stream.ToArray());
                    stream.SetLength(0);
                    try
                    {
                        _inbox.Enqueue(MessageJson.Deserialize<ServerMessage>(json));
                    }
                    catch (Exception ex)
                    {
                        _inbox.Enqueue(new Error("Mensagem inválida do servidor: " + ex.Message));
                    }
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                LastError = ex.Message;
            }

            _inbox.Enqueue(new Error("desconectado"));
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _socket?.Dispose();
            _socket = null;
        }
    }
}
