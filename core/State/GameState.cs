using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleHunter.Core.State;

/// <summary>
/// Estado completo de uma partida. Imutável: toda mudança passa pelo Reducer,
/// que devolve um novo estado. Serializável para replay, teste e reconexão.
/// Guarda só ids de carta; o conteúdo das cartas vem de GameContent.
/// </summary>
public sealed record GameState(
    GameConfig Config,
    GridMap Map,
    IReadOnlyList<Hunter> Hunters,
    IReadOnlyList<Chest> Chests,
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
        string? targetTreasureCardId)
    {
        if (hunters.Count == 0)
            throw new ArgumentException("Uma partida precisa de ao menos um caçador.", nameof(hunters));

        return new GameState(
            config,
            map,
            hunters.ToList(),
            chests.ToList(),
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

    public GameState WithHunter(Hunter hunter) =>
        this with { Hunters = Hunters.Select(h => h.Id == hunter.Id ? hunter : h).ToList() };

    public Chest? ChestAt(Position position) => Chests.FirstOrDefault(c => c.Position == position);

    public GameState WithChest(Chest chest) =>
        this with { Chests = Chests.Select(c => c.Position == chest.Position ? chest : c).ToList() };

    public bool IsOccupied(Position position) =>
        Hunters.Any(h => h.IsActive && h.Position == position);

    /// <summary>Marcado (GDD): carrega o tesouro-alvo na mão; visível a todos no mapa.</summary>
    public bool IsMarked(int hunterId) =>
        TargetTreasureCardId != null && Hunter(hunterId).HasCard(TargetTreasureCardId);
}
