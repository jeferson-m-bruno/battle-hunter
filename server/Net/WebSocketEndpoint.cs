using BattleHunter.Core.Cards;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Serialization;
using BattleHunter.Server.Matchmaking;
using BattleHunter.Server.Persistence;
using BattleHunter.Server.Rooms;

namespace BattleHunter.Server.Net;

/// <summary>
/// /ws: uma conexão por jogador. Auth (anônimo por dispositivo) → guilda (perfil, loja, loadout) → fila → sala.
/// Se o jogador já tem sala ativa ao autenticar, é reconexão: recebe o snapshot inteiro. Ao cair, a sala guarda a cadeira por 60 s.
/// </summary>
public static class WebSocketEndpoint
{
    public static async Task HandleAsync(HttpContext http, IPlayerStore players, IRoomStore rooms, MatchmakingService matchmaking, GameContent content, ILogger log)
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

                await HandleMessageAsync(message, connection, players, rooms, matchmaking, content, ct);
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

    private static async Task HandleMessageAsync(ClientMessage message, Connection connection, IPlayerStore players, IRoomStore rooms, MatchmakingService matchmaking, GameContent content, CancellationToken ct)
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

        var playerId = connection.PlayerId;
        var now = DateTimeOffset.UtcNow;

        async Task<Profile> ProfileAsync() =>
            await players.GetAsync(playerId, ct) ?? throw new InvalidOperationException("jogador sumiu");

        async Task SendProfile(Profile p) =>
            await connection.SendAsync(new ProfileState(p, LevelRules.XpToNextLevel(p, content.Levels)), ct);

        async Task Save(Profile p)
        {
            await players.SaveAsync(p, ct);
            await SendProfile(p);
        }

        switch (message)
        {
            case QueueJoin join:
            {
                if (!Missions.IsValid(content, join.Mission))
                {
                    await connection.SendAsync(new Error($"Missão desconhecida: {join.Mission}"), ct);
                    return;
                }

                if (rooms.FindByPlayer(playerId) != null)
                {
                    await connection.SendAsync(new Error("Você já está numa partida."), ct);
                    return;
                }

                var profile = await ProfileAsync();
                var mission = content.Missions.Get(join.Mission);
                if (profile.Level < mission.RequiredLevel)
                {
                    await connection.SendAsync(new Error($"{mission.Name} exige nível {mission.RequiredLevel}."), ct);
                    return;
                }

                var invalid = LoadoutRules.Validate(profile, profile.Loadout, content.Cards);
                if (invalid != null)
                {
                    await connection.SendAsync(new Error("Loadout inválido: " + invalid), ct);
                    return;
                }

                await matchmaking.JoinAsync(profile, connection, join.Mission);
                return;
            }

            case QueueLeave:
                matchmaking.Leave(playerId);
                await connection.SendAsync(new QueueLeft(), ct);
                return;

            case PlayerAction act:
            {
                var room = rooms.FindByPlayer(playerId);
                if (room == null)
                {
                    await connection.SendAsync(new Rejected("Você não está numa partida."), ct);
                    return;
                }

                var reason = await room.SubmitAsync(playerId, act.Action);
                if (reason != null)
                    await connection.SendAsync(new Rejected(reason), ct);
                return;
            }

            case ProfileRequest:
                await SendProfile(RankRules.EnsureSeason(await ProfileAsync(), RankRules.SeasonOf(now)));
                return;

            case SetAppearance a:
            {
                var profile = await ProfileAsync();
                var name = string.IsNullOrWhiteSpace(a.Name) ? profile.Name : a.Name.Trim();
                await Save(profile with { Name = name[..Math.Min(name.Length, 16)], ColorIndex = Math.Clamp(a.ColorIndex, 0, 3), FaceIndex = Math.Clamp(a.FaceIndex, 0, 3) });
                connection.PlayerName = name;
                return;
            }

            case AllocatePoint ap:
            {
                var profile = await ProfileAsync();
                if (!Enum.TryParse<LevelRules.Stat>(ap.Stat, ignoreCase: true, out var stat))
                {
                    await connection.SendAsync(new Error($"Atributo desconhecido: {ap.Stat}"), ct);
                    return;
                }

                var updated = LevelRules.AllocatePoint(profile, stat);
                if (updated == null)
                {
                    await connection.SendAsync(new Error("Sem pontos livres."), ct);
                    return;
                }

                await Save(updated);
                return;
            }

            case SetLoadout sl:
            {
                if (rooms.FindByPlayer(playerId) != null)
                {
                    await connection.SendAsync(new Error("Não dá para trocar o loadout durante a partida."), ct);
                    return;
                }

                var profile = await ProfileAsync();
                var invalid = LoadoutRules.Validate(profile, sl.Loadout, content.Cards);
                if (invalid != null)
                {
                    await connection.SendAsync(new Error(invalid), ct);
                    return;
                }

                await Save(profile with { Loadout = sl.Loadout });
                return;
            }

            case ShopRequest:
                await connection.SendAsync(new ShopState(now.ToString("yyyy-MM-dd"), Shop.StockFor(now, content.Cards)), ct);
                return;

            case Buy buy:
            {
                var updated = Shop.Buy(await ProfileAsync(), buy.CardId, now, content.Cards);
                if (updated == null)
                {
                    await connection.SendAsync(new Error("Não dá para comprar: fora do estoque do dia ou ouro insuficiente."), ct);
                    return;
                }

                await Save(updated);
                return;
            }

            case Sell sell:
            {
                var updated = Shop.Sell(await ProfileAsync(), sell.CardId, content.Cards);
                if (updated == null)
                {
                    await connection.SendAsync(new Error("Não dá para vender: carta não está livre no inventário."), ct);
                    return;
                }

                await Save(updated);
                return;
            }

            case Deposit dep:
            {
                var updated = Shop.Deposit(await ProfileAsync(), dep.Amount);
                if (updated == null)
                {
                    await connection.SendAsync(new Error("Valor inválido para depósito."), ct);
                    return;
                }

                await Save(updated);
                return;
            }

            case RankingRequest:
            {
                var season = RankRules.SeasonOf(now);
                var top = await players.RankingAsync(season, 50, ct);
                await connection.SendAsync(new Ranking(season, top.Select(p => new RankingEntry(p.Id, p.Name, p.Level, p.RankPoints)).ToList()), ct);
                return;
            }

            default:
                await connection.SendAsync(new Error($"Mensagem não suportada: {message.GetType().Name}"), ct);
                return;
        }
    }
}
