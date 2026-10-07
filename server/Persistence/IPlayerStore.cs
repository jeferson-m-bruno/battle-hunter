using BattleHunter.Core.Progression;
using BattleHunter.Core.State;

namespace BattleHunter.Server.Persistence;

/// <summary>Uma partida concluída, para histórico e ranking.</summary>
public sealed record MatchRecord(
    string RoomId,
    string Mission,
    int Seed,
    int Rounds,
    GameEndReason Reason,
    string? WinnerPlayerId,
    IReadOnlyList<string> PlayerIds,
    DateTimeOffset FinishedAt);

/// <summary>Perfis (GDD: jogador, caçador, inventário, ranking) e histórico de partidas.</summary>
public interface IPlayerStore
{
    Task<Profile> GetOrCreateAsync(string deviceId, string name, CancellationToken ct);
    Task<Profile?> GetAsync(string playerId, CancellationToken ct);
    Task SaveAsync(Profile profile, CancellationToken ct);
    Task RecordMatchAsync(MatchRecord match, CancellationToken ct);
    Task<IReadOnlyList<MatchRecord>> HistoryAsync(string playerId, int limit, CancellationToken ct);
    Task<IReadOnlyList<Profile>> RankingAsync(string season, int limit, CancellationToken ct);
}
