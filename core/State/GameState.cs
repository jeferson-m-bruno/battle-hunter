using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleHunter.Core.State;

/// <summary>
/// Estado completo de uma partida. Imutável: toda mudança passa pelo Reducer,
/// que devolve um novo estado. Serializável para replay, teste e reconexão.
/// Guarda só ids de carta e de tipo de monstro; o conteúdo vem de GameContent.
/// </summary>
public sealed record GameState(
    GameConfig Config,
    GridMap Map,
    IReadOnlyList<Hunter> Hunters,
    IReadOnlyList<Chest> Chests,
    IReadOnlyList<Monster> Monsters,
    IReadOnlyList<Position> MonsterSpawns,
    IReadOnlyList<GroundCard> GroundCards,
    IReadOnlyList<Trap> Traps,
    Alarm? Alarm,
    int NextMonsterId,
    string? TargetTreasureCardId,
    IReadOnlyList<int> TurnOrder,
    int TurnIndex,
    int Round,
    int ActionPoints,
    GamePhase Phase,
    GameEndReason? EndReason,
    int? WinnerId)
{
    public const int MaxHandSize = 10;

    public static GameState New(GameConfig config, GridMap map, IReadOnlyList<Hunter> hunters) =>
        New(config, map, hunters, Array.Empty<Chest>(), targetTreasureCardId: null);

    public static GameState New(
        GameConfig config,
        GridMap map,
        IReadOnlyList<Hunter> hunters,
        IReadOnlyList<Chest> chests,
        string? targetTreasureCardId,
        IReadOnlyList<Monster>? monsters = null,
        IReadOnlyList<Position>? monsterSpawns = null)
    {
        if (hunters.Count == 0)
            throw new ArgumentException("Uma partida precisa de ao menos um caçador.", nameof(hunters));

        var monsterList = (monsters ?? Array.Empty<Monster>()).ToList();
        return new GameState(
            config,
            map,
            hunters.ToList(),
            chests.ToList(),
            monsterList,
            (monsterSpawns ?? Array.Empty<Position>()).ToList(),
            GroundCards: Array.Empty<GroundCard>(),
            Traps: Array.Empty<Trap>(),
            Alarm: null,
            NextMonsterId: monsterList.Count == 0 ? 1 : monsterList.Max(m => m.Id) + 1,
            targetTreasureCardId,
            TurnOrder: Array.Empty<int>(),
            TurnIndex: 0,
            Round: 0,
            ActionPoints: 0,
            Phase: GamePhase.NotStarted,
            EndReason: null,
            WinnerId: null);
    }

    /// <summary>Id do caçador da vez. Inválido antes de StartGame.</summary>
    public int CurrentHunterId => TurnOrder[TurnIndex];

    public Hunter Hunter(int id) => Hunters.First(h => h.Id == id);

    public Hunter? HunterAt(Position position) => Hunters.FirstOrDefault(h => h.IsActive && h.Position == position);

    public GameState WithHunter(Hunter hunter) =>
        this with { Hunters = Hunters.Select(h => h.Id == hunter.Id ? hunter : h).ToList() };

    public Chest? ChestAt(Position position) => Chests.FirstOrDefault(c => c.Position == position);

    public GameState WithChest(Chest chest) =>
        this with { Chests = Chests.Select(c => c.Position == chest.Position ? chest : c).ToList() };

    public Monster? MonsterAt(Position position) => Monsters.FirstOrDefault(m => m.Position == position);

    public Monster Monster(int id) => Monsters.First(m => m.Id == id);

    public bool HasMonster(int id) => Monsters.Any(m => m.Id == id);

    public GameState WithMonster(Monster monster) =>
        this with { Monsters = Monsters.Select(m => m.Id == monster.Id ? monster : m).ToList() };

    public GameState WithMonsterAdded(Monster monster) =>
        this with { Monsters = Monsters.Append(monster).ToList(), NextMonsterId = Math.Max(NextMonsterId, monster.Id + 1) };

    public GameState WithMonsterRemoved(int id) =>
        this with { Monsters = Monsters.Where(m => m.Id != id).ToList() };

    public GameState WithGroundCards(IEnumerable<GroundCard> added) =>
        this with { GroundCards = GroundCards.Concat(added).ToList() };

    public Trap? TrapAt(Position position) => Traps.FirstOrDefault(t => t.Position == position);

    public GameState WithTrapRemoved(Trap trap) =>
        this with { Traps = Traps.Where(t => t != trap).ToList() };

    /// <summary>Ocupada por um caçador ativo ou por um monstro.</summary>
    public bool IsOccupied(Position position) =>
        HunterAt(position) != null || MonsterAt(position) != null;

    /// <summary>Marcado (GDD): carrega o tesouro-alvo na mão; visível a todos no mapa.</summary>
    public bool IsMarked(int hunterId) =>
        TargetTreasureCardId != null && Hunter(hunterId).HasCard(TargetTreasureCardId);
}
