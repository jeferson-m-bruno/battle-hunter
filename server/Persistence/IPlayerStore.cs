using BattleHunter.Core.State;

namespace BattleHunter.Server.Persistence;

/// <summary>Perfil persistente do jogador (GDD: jogador + caçador; inventário e ranking na fatia 7).</summary>
public sealed record PlayerRecord(string Id, string DeviceId, string Name, int Level, int Xp, int Gold);

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

public interface IPlayerStore
{
    Task<PlayerRecord> GetOrCreateAsync(string deviceId, string name, CancellationToken ct);
    Task<PlayerRecord?> GetAsync(string playerId, CancellationToken ct);
    Task RecordMatchAsync(MatchRecord match, CancellationToken ct);
    Task<IReadOnlyList<MatchRecord>> HistoryAsync(string playerId, int limit, CancellationToken ct);
}
