using BattleHunter.Core.Serialization;
using BattleHunter.Server.Matchmaking;
using BattleHunter.Server.Persistence;
using BattleHunter.Server.Rooms;

namespace BattleHunter.Server.Net;

/// <summary>
/// /ws: uma conexão por jogador. Auth (anônimo por dispositivo) → fila → sala. Se o jogador já tem sala ativa
/// ao autenticar, é reconexão: recebe o snapshot inteiro. Ao cair, a sala guarda a cadeira por 60 s.
/// </summary>
public static class WebSocketEndpoint
{
    public static async Task HandleAsync(HttpContext http, IPlayerStore players, IRoomStore rooms, MatchmakingService matchmaking, ILogger log)
    {
        if (!http.WebSockets.IsWebSocketRequest)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsync("WebSocket esperado em /ws");
            return;
        }

        using var socket = await http.WebSockets.AcceptWebSocketAsync();
        await using var connection = new Connection(socket, log);
        Metrics.ConnectionsOpened++;
        var ct = http.RequestAborted;

        try
        {
            while (true)
            {
                var message = await connection.ReceiveAsync(ct);
                if (message == null)
                    break;

                await HandleMessageAsync(message, connection, players, rooms, matchmaking, ct);
            }
        }
        finally
        {
            if (connection.PlayerId != null)
            {
                matchmaking.Leave(connection.PlayerId);
                rooms.FindByPlayer(connection.PlayerId)?.Disconnect(connection.PlayerId);
            }

            await connection.CloseAsync("tchau");
        }
    }

    private static async Task HandleMessageAsync(ClientMessage message, Connection connection, IPlayerStore players, IRoomStore rooms, MatchmakingService matchmaking, CancellationToken ct)
    {
        switch (message)
        {
            case Auth auth:
                if (string.IsNullOrWhiteSpace(auth.DeviceId))
                {
                    await connection.SendAsync(new Error("deviceId obrigatório"), ct);
                    return;
                }

                var player = await players.GetOrCreateAsync(auth.DeviceId.Trim(), string.IsNullOrWhiteSpace(auth.Name) ? "Caçador" : auth.Name.Trim(), ct);
                connection.PlayerId = player.Id;
                connection.PlayerName = player.Name;
                await connection.SendAsync(new Welcome(player.Id, player.Name, player.Level), ct);

                var active = rooms.FindByPlayer(player.Id);
                if (active != null)
                    await active.ReconnectAsync(player.Id, connection);
                return;

            case Ping ping:
                await connection.SendAsync(new Pong(ping.Sent, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), ct);
                return;
        }

        if (connection.PlayerId == null)
        {
            await connection.SendAsync(new Error("Autentique primeiro (Auth)."), ct);
            return;
        }

        switch (message)
        {
            case QueueJoin join:
                if (!Missions.IsValid(join.Mission))
                {
                    await connection.SendAsync(new Error($"Missão desconhecida: {join.Mission}"), ct);
                    return;
                }

                if (rooms.FindByPlayer(connection.PlayerId) != null)
                {
                    await connection.SendAsync(new Error("Você já está numa partida."), ct);
                    return;
                }

                var record = await players.GetAsync(connection.PlayerId, ct) ?? throw new InvalidOperationException("jogador sumiu");
                await matchmaking.JoinAsync(record, connection, join.Mission);
                return;

            case QueueLeave:
                matchmaking.Leave(connection.PlayerId);
                await connection.SendAsync(new QueueLeft(), ct);
                return;

            case PlayerAction act:
                var room = rooms.FindByPlayer(connection.PlayerId);
                if (room == null)
                {
                    await connection.SendAsync(new Rejected("Você não está numa partida."), ct);
                    return;
                }

                var reason = await room.SubmitAsync(connection.PlayerId, act.Action);
                if (reason != null)
                    await connection.SendAsync(new Rejected(reason), ct);
                return;

            default:
                await connection.SendAsync(new Error($"Mensagem não suportada: {message.GetType().Name}"), ct);
                return;
        }
    }
}
