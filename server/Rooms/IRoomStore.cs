using System.Collections.Concurrent;

namespace BattleHunter.Server.Rooms;

/// <summary>
/// Onde as salas vivem. Em memória por enquanto (uma instância por região, GDD);
/// escalar horizontalmente = implementar com Redis atrás desta interface.
/// </summary>
public interface IRoomStore
{
    void Add(Room room);
    Room? Get(string roomId);
    Room? FindByPlayer(string playerId);
    void Remove(string roomId);
    int Count { get; }
    IReadOnlyCollection<Room> All { get; }
}

public sealed class InMemoryRoomStore : IRoomStore
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new(StringComparer.Ordinal);

    public void Add(Room room) => _rooms[room.Id] = room;

    public Room? Get(string roomId) => _rooms.TryGetValue(roomId, out var room) ? room : null;

    public Room? FindByPlayer(string playerId) => _rooms.Values.FirstOrDefault(r => !r.IsFinished && r.HasPlayer(playerId));

    public void Remove(string roomId) => _rooms.TryRemove(roomId, out _);

    public int Count => _rooms.Count;

    public IReadOnlyCollection<Room> All => _rooms.Values.ToList();
}
