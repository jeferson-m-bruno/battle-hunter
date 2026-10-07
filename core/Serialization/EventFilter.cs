using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Serialization;

/// <summary>
/// Decide o que cada caçador recebe de um lote de eventos: cartas sacadas/roubadas e armadilhas só aos envolvidos;
/// movimentos e ataques fora da visão omitidos (o Marcado é sempre visível). O servidor chama por destinatário.
/// </summary>
public static class EventFilter
{
    public static IReadOnlyList<GameEvent> ForRecipient(IReadOnlyList<GameEvent> events, GameState stateAfter, int recipientId, GameContent content)
    {
        var view = HunterView.For(stateAfter, recipientId, content);
        var visible = new HashSet<Position>(view.VisibleCells);
        var result = new List<GameEvent>(events.Count);

        foreach (var e in events)
        {
            switch (e)
            {
                case CardDrawn d:
                    if (d.HunterId == recipientId)
                        result.Add(d);
                    break;
                case CardStolen s:
                    result.Add(s.ThiefId == recipientId || s.VictimId == recipientId ? s : s with { CardId = "" });
                    break;
                case TrapPlaced t:
                    if (t.OwnerId == recipientId)
                        result.Add(t);
                    break;
                case HunterMoved m:
                    if (m.HunterId == recipientId || stateAfter.IsMarked(m.HunterId) || visible.Contains(m.From) || visible.Contains(m.To))
                        result.Add(m);
                    break;
                case MonsterMoved m:
                    if (visible.Contains(m.From) || visible.Contains(m.To))
                        result.Add(m);
                    break;
                case MonsterSpawned m:
                    if (visible.Contains(m.Position))
                        result.Add(m);
                    break;
                case MonsterStateChanged m:
                    if (IsMonsterVisible(stateAfter, visible, m.MonsterId))
                        result.Add(m);
                    break;
                case AttackResolved a:
                    if (Involves(a.Attacker, recipientId) || Involves(a.Target, recipientId) || IsVisible(stateAfter, visible, a.Attacker) || IsVisible(stateAfter, visible, a.Target))
                        result.Add(a);
                    break;
                case DamageDealt d:
                    if (Involves(d.Source, recipientId) || Involves(d.Target, recipientId) || IsVisible(stateAfter, visible, d.Target))
                        result.Add(d);
                    break;
                default:
                    result.Add(e);
                    break;
            }
        }

        return result;
    }

    private static bool Involves(Combatant c, int hunterId) => c.Kind == CombatantKind.Hunter && c.Id == hunterId;

    private static bool IsVisible(GameState state, HashSet<Position> visible, Combatant c) =>
        c.Kind == CombatantKind.Monster
            ? IsMonsterVisible(state, visible, c.Id)
            : c.Kind == CombatantKind.Hunter && (state.IsMarked(c.Id) || (state.Hunter(c.Id).IsActive && visible.Contains(state.Hunter(c.Id).Position)));

    private static bool IsMonsterVisible(GameState state, HashSet<Position> visible, int monsterId) =>
        state.HasMonster(monsterId) && visible.Contains(state.Monster(monsterId).Position);
}
