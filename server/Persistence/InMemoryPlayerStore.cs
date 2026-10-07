using System.Collections.Concurrent;

namespace BattleHunter.Server.Persistence;

/// <summary>Loja em memória: padrão sem connection string (desenvolvimento, testes, CI). Some ao reiniciar.</summary>
public sealed class InMemoryPlayerStore : IPlayerStore
{
    private readonly ConcurrentDictionary<string, PlayerRecord> _byDevice = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PlayerRecord> _byId = new(StringComparer.Ordinal);
    private readonly ConcurrentBag<MatchRecord> _matches = new();

    public Task<PlayerRecord> GetOrCreateAsync(string deviceId, string name, CancellationToken ct)
    {
        var record = _byDevice.GetOrAdd(deviceId, _ =>
        {
            var created = new PlayerRecord(Guid.NewGuid().ToString("N"), deviceId, name, Level: 1, Xp: 0, Gold: 0);
            _byId[created.Id] = created;
            return created;
        });

        if (record.Name != name && !string.IsNullOrWhiteSpace(name))
        {
            record = record with { Name = name };
            _byDevice[deviceId] = record;
            _byId[record.Id] = record;
        }

        return Task.FromResult(record);
    }

    public Task<PlayerRecord?> GetAsync(string playerId, CancellationToken ct) =>
        Task.FromResult(_byId.TryGetValue(playerId, out var r) ? r : null);

    public Task RecordMatchAsync(MatchRecord match, CancellationToken ct)
    {
        _matches.Add(match);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MatchRecord>> HistoryAsync(string playerId, int limit, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MatchRecord>>(_matches.Where(m => m.PlayerIds.Contains(playerId)).OrderByDescending(m => m.FinishedAt).Take(limit).ToList());
}
