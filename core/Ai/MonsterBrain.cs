using System;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Ai;

/// <summary>O que um monstro decidiu fazer nesta fase.</summary>
/// <param name="State">Novo estado da FSM.</param>
/// <param name="TargetHunterId">Caçador perseguido/atacado, se houver.</param>
/// <param name="Destination">Para onde andar quando perseguindo (caçador ou alarme).</param>
/// <param name="IsPassable">Restrição de movimento (o Esqueleto não sai da sala).</param>
public sealed record MonsterDecision(
    MonsterState State,
    int? TargetHunterId,
    Position? Destination,
    Func<Position, bool>? IsPassable);

/// <summary>
/// FSM dos monstros (GDD): Ocioso → Perseguindo quando há caçador em linha de visão; Perseguindo → Atacando
/// quando adjacente; sem alvo à vista volta a Ocioso (ou vai ao Alarme). Comportamento por tipo:
/// Kobold/Aranha/Mímico/Dragão perseguem o mais próximo, Orc o mais ferido, Esqueleto só dentro da sala.
/// </summary>
public static class MonsterBrain
{
    public static MonsterDecision Decide(GameState state, Monster monster, GameContent content)
    {
        var type = content.Monsters.Get(monster.TypeId);
        var room = type.Behavior == MonsterBehavior.GuardRoom ? state.Map.RoomAt(monster.Position) : null;
        Func<Position, bool>? passable = room != null ? p => room.Contains(p) : null;

        var visible = state.Hunters
            .Where(h => h.IsActive && LineOfSight.IsClear(state.Map, monster.Position, h.Position))
            .Where(h => room == null || room.Contains(h.Position))
            .ToList();

        var target = type.Behavior == MonsterBehavior.ChaseWounded
            ? visible.OrderBy(h => h.Hp * 100 / Math.Max(1, h.Stats.MaxHp)).ThenBy(h => h.Position.DistanceTo(monster.Position)).ThenBy(h => h.Id).FirstOrDefault()
            : visible.OrderBy(h => h.Position.DistanceTo(monster.Position)).ThenBy(h => h.Id).FirstOrDefault();

        if (target != null)
        {
            return monster.Position.IsOrthogonallyAdjacentTo(target.Position)
                ? new MonsterDecision(MonsterState.Attacking, target.Id, null, passable)
                : new MonsterDecision(MonsterState.Chasing, target.Id, target.Position, passable);
        }

        if (state.Alarm != null && room == null)
            return new MonsterDecision(MonsterState.Chasing, null, state.Alarm.Position, passable);

        return new MonsterDecision(MonsterState.Idle, null, null, passable);
    }
}
