using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Rules;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Server.Net;
using BattleHunter.Server.Persistence;

namespace BattleHunter.Server.Rooms;

/// <summary>Quem ocupa um dos 4 caçadores: um jogador (conectado ou não) ou a IA.</summary>
public sealed class Seat
{
    public int HunterId { get; init; }
    public string? PlayerId { get; init; }
    public string Name { get; init; } = "";
    public AiProfile Profile { get; init; }
    /// <summary>Perfil do jogador na entrada: nível, stats, equipamento e cartas levadas.</summary>
    public Profile? PlayerProfile { get; init; }
    public Connection? Connection { get; set; }
    public DateTimeOffset? DisconnectedAt { get; set; }
    public AiPlayer? Ai { get; set; }

    public bool IsAiSeat => PlayerId == null;
}

/// <summary>
/// Uma sala = um GameState + fila de ações. O servidor é a única fonte da verdade: rola dados, valida pelo Reducer
/// e manda a cada jogador só o que ele pode ver. Timer de turno, IA para vagas e ausentes, reconexão com snapshot.
/// No fim, aplica as recompensas da missão no perfil de cada jogador.
/// </summary>
public sealed class Room
{
    private readonly GameContent _content;
    private readonly ServerOptions _options;
    private readonly IPlayerStore _players;
    private readonly ILogger _log;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<int, Seat> _seats;
    private readonly IRandom _random;
    private readonly CancellationTokenSource _cts = new();
    private DateTimeOffset _turnStartedAt = DateTimeOffset.UtcNow;
    private int _turnKey = -1;

    public Room(string id, string mission, int seed, IReadOnlyList<Seat> seats, GameContent content, ServerOptions options, IPlayerStore players, ILogger log)
    {
        Id = id;
        Mission = mission;
        Seed = seed;
        _content = content;
        _options = options;
        _players = players;
        _log = log;
        _seats = seats.ToDictionary(s => s.HunterId);
        _random = new SeededRandom(seed);

        MissionType = content.Missions.Get(mission);
        var settings = MatchSettings.For(MissionType);
        var map = MapGenerator.Generate(settings.Map, _random);
        var commons = content.Cards.Where(c => c.Rarity == Rarity.Common && c.Type != CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        var treasures = content.Cards.Where(c => c.Type == CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();

        var setups = seats.OrderBy(s => s.HunterId).Select(s =>
        {
            if (s.PlayerProfile != null)
                return LoadoutRules.ToHunterSetup(s.PlayerProfile, s.HunterId);

            // IA: 2 cartas comuns, como na simulação.
            var hand = Enumerable.Range(0, settings.StartingCommonCards).Select(_ => commons[_random.Next(commons.Count)].Id).ToList();
            return new HunterSetup(s.HunterId, s.Name, HunterStats.Base, hand);
        }).ToList();

        State = GameSetup.Create(settings.Config, map, setups, treasures[_random.Next(treasures.Count)].Id, _random, content);
        foreach (var seat in seats.Where(s => s.IsAiSeat))
            seat.Ai = new AiPlayer(seat.HunterId, seat.Profile);
    }

    public string Id { get; }
    public string Mission { get; }
    public MissionType MissionType { get; }
    public int Seed { get; }
    public GameState State { get; private set; }
    public bool IsFinished => State.Phase == GamePhase.Finished;
    public IReadOnlyCollection<Seat> Seats => _seats.Values;
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public bool HasPlayer(string playerId) => _seats.Values.Any(s => s.PlayerId == playerId);

    public Seat? SeatOf(string playerId) => _seats.Values.FirstOrDefault(s => s.PlayerId == playerId);

    public PlayerSnapshot SnapshotFor(int hunterId) => PlayerSnapshot.For(State, hunterId, _content);

    /// <summary>Abre a partida: StartGame, MatchStarted para todos e o laço de turnos em segundo plano.</summary>
    public async Task StartAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var result = Reducer.Apply(State, new StartGame(), _random, _content);
            State = result.State;
            ResetTurnClock();
            foreach (var seat in _seats.Values.Where(s => s.Connection != null))
                await seat.Connection!.SendAsync(new MatchStarted(Id, seat.HunterId, SnapshotFor(seat.HunterId)));
        }
        finally
        {
            _lock.Release();
        }

        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    /// <summary>Intenção de um jogador. Devolve o motivo da recusa, ou null.</summary>
    public async Task<string?> SubmitAsync(string playerId, GameAction action)
    {
        var seat = SeatOf(playerId);
        if (seat == null)
            return "Você não está nesta sala.";

        if (action is HunterAction ha && ha.HunterId != seat.HunterId)
            return "Essa ação não é do seu caçador.";

        if (action is StartGame)
            return "A partida já começou.";

        await _lock.WaitAsync();
        try
        {
            if (IsFinished)
                return "A partida já terminou.";

            if (State.CurrentHunterId != seat.HunterId)
                return "Não é a sua vez.";

            return await ApplyAsync(action);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ReconnectAsync(string playerId, Connection connection)
    {
        var seat = SeatOf(playerId);
        if (seat == null)
            return;

        await _lock.WaitAsync();
        try
        {
            seat.Connection = connection;
            seat.DisconnectedAt = null;
            seat.Ai = null;
            await connection.SendAsync(new MatchResumed(Id, seat.HunterId, SnapshotFor(seat.HunterId)));
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Disconnect(string playerId)
    {
        var seat = SeatOf(playerId);
        if (seat == null)
            return;

        seat.Connection = null;
        seat.DisconnectedAt ??= DateTimeOffset.UtcNow;
    }

    public void Stop() => _cts.Cancel();

    // ---- laço de turnos ------------------------------------------------------

    private async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && !IsFinished)
            {
                await _lock.WaitAsync(ct);
                try
                {
                    if (IsFinished)
                        break;

                    var seat = _seats[State.CurrentHunterId];
                    var ai = AiFor(seat);
                    if (ai != null)
                    {
                        _lock.Release();
                        await Task.Delay(_options.AiDelayMs, ct);
                        await _lock.WaitAsync(ct);
                        if (IsFinished || State.CurrentHunterId != seat.HunterId)
                            continue;

                        var action = ai.Next(State, _content);
                        if (await ApplyAsync(action) != null)
                            await ApplyAsync(State.Phase == GamePhase.AwaitingRoll ? new RollDice(seat.HunterId) : new Pass(seat.HunterId));
                        continue;
                    }

                    if (DateTimeOffset.UtcNow - _turnStartedAt >= TimeSpan.FromSeconds(_options.TurnSeconds))
                        await ApplyAsync(State.Phase == GamePhase.AwaitingRoll ? new RollDice(seat.HunterId) : new Pass(seat.HunterId));
                }
                finally
                {
                    if (_lock.CurrentCount == 0)
                        _lock.Release();
                }

                await Task.Delay(100, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Sala {Room} caiu", Id);
        }
    }

    /// <summary>IA da cadeira: vaga de IA, ou jogador desconectado há mais que a tolerância.</summary>
    private AiPlayer? AiFor(Seat seat)
    {
        if (seat.IsAiSeat)
            return seat.Ai;

        if (seat.Connection == null && seat.DisconnectedAt != null &&
            DateTimeOffset.UtcNow - seat.DisconnectedAt.Value >= TimeSpan.FromSeconds(_options.ReconnectGraceSeconds))
            return seat.Ai ??= new AiPlayer(seat.HunterId, AiProfile.Balanced);

        return null;
    }

    /// <summary>Aplica no Reducer (sob o lock), publica eventos filtrados + snapshot e encerra a partida se acabou.</summary>
    private async Task<string?> ApplyAsync(GameAction action)
    {
        var result = Reducer.Apply(State, action, _random, _content);
        var rejected = result.Events.OfType<ActionRejected>().FirstOrDefault();
        if (rejected != null)
            return rejected.Reason;

        State = result.State;
        ResetTurnClock();

        foreach (var seat in _seats.Values.Where(s => s.Connection != null))
        {
            var events = EventFilter.ForRecipient(result.Events, State, seat.HunterId, _content);
            await seat.Connection!.SendAsync(new MatchUpdate(events, SnapshotFor(seat.HunterId)));
        }

        if (IsFinished)
            await FinishAsync();

        return null;
    }

    private void ResetTurnClock()
    {
        var key = State.Round * 16 + State.TurnIndex;
        if (key != _turnKey || State.Phase == GamePhase.AwaitingRoll)
        {
            _turnKey = key;
            _turnStartedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task FinishAsync()
    {
        Metrics.MatchesFinished++;
        foreach (var seat in _seats.Values.Where(s => s.Connection != null))
            await seat.Connection!.SendAsync(new GameOver(State.EndReason ?? GameEndReason.RoundLimit, State.WinnerId, SnapshotFor(seat.HunterId)));

        var winnerSeat = State.WinnerId != null ? _seats[State.WinnerId.Value] : null;
        var season = RankRules.SeasonOf(DateTimeOffset.UtcNow);

        // Recompensas (GDD: fim de missão) no perfil de cada jogador humano.
        foreach (var seat in _seats.Values.Where(s => s.PlayerId != null))
        {
            try
            {
                var profile = await _players.GetAsync(seat.PlayerId!, CancellationToken.None);
                if (profile == null)
                    continue;

                var final = State.Hunter(seat.HunterId);
                var (updated, summary) = MatchRewards.Apply(profile, final, State.WinnerId == seat.HunterId, MissionType, _content, _random, season);
                await _players.SaveAsync(updated, CancellationToken.None);
                if (seat.Connection != null)
                    await seat.Connection.SendAsync(new MatchRewarded(summary, updated));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Falha ao recompensar {Player} na sala {Room}", seat.PlayerId, Id);
            }
        }

        try
        {
            await _players.RecordMatchAsync(new MatchRecord(
                Id, Mission, Seed, State.Round, State.EndReason ?? GameEndReason.RoundLimit,
                winnerSeat?.PlayerId,
                _seats.Values.Where(s => s.PlayerId != null).Select(s => s.PlayerId!).ToList(),
                DateTimeOffset.UtcNow), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falha ao gravar a partida {Room}", Id);
        }

        _cts.Cancel();
    }
}
