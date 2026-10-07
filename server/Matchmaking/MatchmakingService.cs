using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Serialization;
using BattleHunter.Server.Net;
using BattleHunter.Server.Persistence;
using BattleHunter.Server.Rooms;
using Microsoft.Extensions.Options;

namespace BattleHunter.Server.Matchmaking;

/// <summary>Jogador esperando sala.</summary>
public sealed record QueueEntry(Profile Profile, Connection Connection, string Mission, DateTimeOffset JoinedAt)
{
    public string PlayerId => Profile.Id;
    public int Level => Profile.Level;
}

/// <summary>
/// Fila por tipo de missão (GDD): 4 jogadores na faixa de nível ±4 formam sala; após 30 s sem sala cheia,
/// a IA completa as vagas. Um tique por segundo.
/// </summary>
public sealed class MatchmakingService : BackgroundService
{
    private readonly object _gate = new();
    private readonly List<QueueEntry> _queue = new();
    private readonly IRoomStore _rooms;
    private readonly IPlayerStore _players;
    private readonly GameContent _content;
    private readonly ServerOptions _options;
    private readonly ILogger<MatchmakingService> _log;
    private int _seedCounter = Environment.TickCount & 0x7fffffff;

    public MatchmakingService(IRoomStore rooms, IPlayerStore players, GameContent content, IOptions<ServerOptions> options, ILogger<MatchmakingService> log)
    {
        _rooms = rooms;
        _players = players;
        _content = content;
        _options = options.Value;
        _log = log;
    }

    public int Waiting(string mission)
    {
        lock (_gate)
            return _queue.Count(e => e.Mission == mission);
    }

    public async Task JoinAsync(Profile profile, Connection connection, string mission)
    {
        lock (_gate)
        {
            _queue.RemoveAll(e => e.PlayerId == profile.Id);
            _queue.Add(new QueueEntry(profile, connection, mission, DateTimeOffset.UtcNow));
        }

        await connection.SendAsync(new Queued(mission, Waiting(mission), _options.QueueFillSeconds));
    }

    public void Leave(string playerId)
    {
        lock (_gate)
            _queue.RemoveAll(e => e.PlayerId == playerId);
    }

    /// <summary>Forma as salas possíveis agora. Público para os testes forçarem um tique.</summary>
    public async Task TickAsync()
    {
        List<List<QueueEntry>> groups = new();
        lock (_gate)
        {
            foreach (var mission in Missions.Ids(_content))
            {
                var waiting = _queue.Where(e => e.Mission == mission && e.Connection.IsOpen).OrderBy(e => e.JoinedAt).ToList();
                _queue.RemoveAll(e => e.Mission == mission && !e.Connection.IsOpen);

                while (waiting.Count > 0)
                {
                    var group = Compatible(waiting, mission);
                    var oldest = group[0];
                    var waited = DateTimeOffset.UtcNow - oldest.JoinedAt;
                    if (group.Count < BattleHunter.Core.Map.MapSpec.HunterSpawns && waited < TimeSpan.FromSeconds(_options.QueueFillSeconds))
                        break;

                    groups.Add(group);
                    foreach (var e in group)
                    {
                        waiting.Remove(e);
                        _queue.Remove(e);
                    }
                }
            }
        }

        foreach (var group in groups)
            await CreateRoomAsync(group);
    }

    /// <summary>Até 4 jogadores compatíveis com o mais antigo: ranqueada ±2 níveis, casual ±LevelRange.</summary>
    private List<QueueEntry> Compatible(List<QueueEntry> waiting, string mission)
    {
        var anchor = waiting[0];
        var range = _content.Missions.Get(mission).Ranked ? 2 : _options.LevelRange;
        return waiting.Where(e => Math.Abs(e.Level - anchor.Level) <= range).Take(BattleHunter.Core.Map.MapSpec.HunterSpawns).ToList();
    }

    private async Task CreateRoomAsync(List<QueueEntry> group)
    {
        var mission = group[0].Mission;
        var seed = Interlocked.Increment(ref _seedCounter);
        var profiles = new[] { AiProfile.Aggressive, AiProfile.Cautious, AiProfile.Greedy, AiProfile.Balanced };
        var seats = new List<Seat>();

        for (var i = 0; i < BattleHunter.Core.Map.MapSpec.HunterSpawns; i++)
        {
            if (i < group.Count)
            {
                var e = group[i];
                // Perfil mais recente: a guilda pode ter mudado o loadout depois de entrar na fila.
                var profile = await _players.GetAsync(e.PlayerId, CancellationToken.None) ?? e.Profile;
                seats.Add(new Seat { HunterId = i + 1, PlayerId = e.PlayerId, Name = profile.Name, Profile = AiProfile.Balanced, PlayerProfile = profile, Connection = e.Connection });
            }
            else
            {
                var profile = profiles[(seed + i) % profiles.Length];
                seats.Add(new Seat { HunterId = i + 1, PlayerId = null, Name = $"IA {profile}", Profile = profile });
            }
        }

        var room = new Room(Guid.NewGuid().ToString("N")[..8], mission, seed, seats, _content, _options, _players, _log);
        _rooms.Add(room);
        Metrics.MatchesStarted++;
        _log.LogInformation("Sala {Room} ({Mission}, seed {Seed}) com {Humans} jogador(es) e {Ai} IA(s)", room.Id, mission, seed, group.Count, BattleHunter.Core.Map.MapSpec.HunterSpawns - group.Count);
        await room.StartAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync();
                foreach (var room in _rooms.All.Where(r => r.IsFinished && DateTimeOffset.UtcNow - r.StartedAt > TimeSpan.FromMinutes(5)))
                    _rooms.Remove(room.Id);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Matchmaking falhou num tique");
            }

            try
            {
                await Task.Delay(_options.MatchmakingTickMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
