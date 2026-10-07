using System.Collections.Concurrent;
using BattleHunter.Core.Progression;

namespace BattleHunter.Server.Persistence;

/// <summary>Loja em memória: padrão sem connection string (desenvolvimento, testes, CI). Some ao reiniciar.</summary>
public sealed class InMemoryPlayerStore : IPlayerStore
{
    private readonly ConcurrentDictionary<string, string> _idByDevice = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Profile> _byId = new(StringComparer.Ordinal);
    private readonly ConcurrentBag<MatchRecord> _matches = new();

    public Task<Profile> GetOrCreateAsync(string deviceId, string name, CancellationToken ct)
    {
        var id = _idByDevice.GetOrAdd(deviceId, _ =>
        {
            var created = Profile.New(Guid.NewGuid().ToString("N"), name);
            _byId[created.Id] = created;
            return created.Id;
        });

        var profile = _byId[id];
        if (!string.IsNullOrWhiteSpace(name) && profile.Name != name)
        {
            profile = profile with { Name = name };
            _byId[id] = profile;
        }

        return Task.FromResult(profile);
    }

    public Task<Profile?> GetAsync(string playerId, CancellationToken ct) =>
        Task.FromResult(_byId.TryGetValue(playerId, out var p) ? p : null);

    public Task SaveAsync(Profile profile, CancellationToken ct)
    {
        _byId[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public Task RecordMatchAsync(MatchRecord match, CancellationToken ct)
    {
        _matches.Add(match);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MatchRecord>> HistoryAsync(string playerId, int limit, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MatchRecord>>(_matches.Where(m => m.PlayerIds.Contains(playerId)).OrderByDescending(m => m.FinishedAt).Take(limit).ToList());

    public Task<IReadOnlyList<Profile>> RankingAsync(string season, int limit, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Profile>>(_byId.Values.Where(p => p.Season == season && p.RankPoints > 0).OrderByDescending(p => p.RankPoints).ThenBy(p => p.Name).Take(limit).ToList());
}
