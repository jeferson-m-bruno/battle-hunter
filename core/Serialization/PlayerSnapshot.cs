using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Serialization;

/// <summary>Outro caçador como o cliente o recebe: nome, nível, equipamento e tamanho da mão sempre; posição e PV só se visível ou Marcado.</summary>
public sealed record HunterSummary(
    int Id,
    string Name,
    int Level,
    HunterStatus Status,
    Equipment Equipment,
    int HandCount,
    bool IsMarked,
    Position? Position,
    int? Hp);

/// <summary>
/// Tudo que um cliente pode saber da partida, do ponto de vista de um caçador (GDD: névoa, mão oculta, armadilhas próprias).
/// O servidor manda um snapshot a cada mudança; o cliente nunca recebe o GameState inteiro.
/// </summary>
public sealed record PlayerSnapshot(
    int HunterId,
    GridMap Map,
    Hunter Self,
    HunterStats EffectiveStats,
    IReadOnlyList<HunterSummary> Hunters,
    IReadOnlyList<Monster> Monsters,
    IReadOnlyList<GroundCard> GroundCards,
    IReadOnlyList<Trap> OwnTraps,
    IReadOnlyList<Chest> Chests,
    IReadOnlyList<Position> VisibleCells,
    IReadOnlyList<int> TurnOrder,
    int? CurrentHunterId,
    int Round,
    int MaxRounds,
    int ActionPoints,
    GamePhase Phase,
    bool IsMarked,
    int? MarkedHunterId,
    bool BossPresent,
    Position? BossPosition,
    GameEndReason? EndReason,
    int? WinnerId)
{
    public static PlayerSnapshot For(GameState state, int hunterId, GameContent content)
    {
        var view = HunterView.For(state, hunterId, content);
        var self = state.Hunter(hunterId);

        var hunters = state.Hunters.Select(h =>
        {
            if (h.Id == hunterId)
                return new HunterSummary(h.Id, h.Name, h.Level, h.Status, h.Equipment, h.Hand.Count, view.IsMarked, h.Position, h.Hp);

            var seen = view.Others.First(o => o.Id == h.Id);
            return new HunterSummary(h.Id, h.Name, h.Level, h.Status, h.Equipment, h.Hand.Count, seen.IsMarked, seen.Position, seen.Hp);
        }).ToList();

        var marked = state.Hunters.FirstOrDefault(h => h.IsActive && state.IsMarked(h.Id));

        return new PlayerSnapshot(
            hunterId,
            state.Map,
            self,
            view.Stats,
            hunters,
            view.Monsters,
            view.GroundCards,
            view.OwnTraps,
            view.Chests,
            view.VisibleCells.ToList(),
            state.TurnOrder,
            state.TurnOrder.Count > 0 ? state.CurrentHunterId : null,
            state.Round,
            state.Config.MaxRounds,
            state.ActionPoints,
            state.Phase,
            view.IsMarked,
            marked?.Id,
            view.BossPresent,
            view.BossPosition,
            state.EndReason,
            state.WinnerId);
    }
}
