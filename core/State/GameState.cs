using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleHunter.Core.State;

/// <summary>
/// Estado completo de uma partida. Imutável: toda mudança passa pelo Reducer,
/// que devolve um novo estado. Serializável para replay, teste e reconexão.
/// </summary>
public sealed record GameState(
    GameConfig Config,
    GridMap Map,
    IReadOnlyList<Hunter> Hunters,
    IReadOnlyList<int> TurnOrder,
    int TurnIndex,
    int Round,
    int ActionPoints,
    GamePhase Phase,
    GameEndReason? EndReason)
{
    public static GameState New(GameConfig config, GridMap map, IReadOnlyList<Hunter> hunters)
    {
        if (hunters.Count == 0)
            throw new ArgumentException("Uma partida precisa de ao menos um caçador.", nameof(hunters));

        return new GameState(
            config,
            map,
            hunters.ToList(),
            TurnOrder: Array.Empty<int>(),
            TurnIndex: 0,
            Round: 0,
            ActionPoints: 0,
            Phase: GamePhase.NotStarted,
            EndReason: null);
    }

    /// <summary>Id do caçador da vez. Inválido antes de StartGame.</summary>
    public int CurrentHunterId => TurnOrder[TurnIndex];

    public Hunter Hunter(int id) => Hunters.First(h => h.Id == id);

    public GameState WithHunter(Hunter hunter) =>
        this with { Hunters = Hunters.Select(h => h.Id == hunter.Id ? hunter : h).ToList() };

    public bool IsOccupied(Position position) =>
        Hunters.Any(h => h.IsActive && h.Position == position);
}
