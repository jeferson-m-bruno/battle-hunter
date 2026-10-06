using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>
/// Disparo de armadilhas ao entrar na célula: caçadores (menos o dono) e monstros.
/// Params da carta: `damage`, `net` (perde o próximo turno), `poison` (só caçadores), `alarm` (só caçadores; rodadas).
/// </summary>
internal static class TrapRules
{
    public const int PoisonTurns = 3;

    /// <summary>Dispara a armadilha da célula, se houver uma que afete a criatura; remove-a do mapa.</summary>
    public static GameState TriggerIfAny(GameState state, Combatant victim, Position cell, IRandom random, GameContent content, List<GameEvent> events)
    {
        var trap = state.TrapAt(cell);
        if (trap == null)
            return state;

        if (victim.Kind == CombatantKind.Hunter && victim.Id == trap.OwnerId)
            return state;

        var card = content.Cards.Get(trap.CardId);
        var owner = Combatant.HunterRef(trap.OwnerId);
        state = state.WithTrapRemoved(trap);
        events.Add(new TrapTriggered(cell, trap.CardId, trap.OwnerId, victim));

        var damage = card.Param("damage");
        if (damage > 0)
        {
            state = CombatRules.ApplyDamage(state, owner, victim, damage, random, content, events,
                hpLeft => new DamageDealt(owner, victim, damage, hpLeft, trap.CardId));
        }

        if (card.Param("net") == 1)
            state = Entangle(state, victim, events);

        if (victim.Kind != CombatantKind.Hunter)
            return state;

        if (card.Param("poison") == 1 && state.Hunter(victim.Id).IsActive)
            state = StatusRules.Apply(state, victim.Id, StatusKind.Poisoned, PoisonTurns, events);

        var alarmRounds = card.Param("alarm");
        if (alarmRounds > 0)
        {
            events.Add(new AlarmRaised(cell, alarmRounds));
            state = state with { Alarm = new Alarm(cell, alarmRounds) };
        }

        return state;
    }

    private static GameState Entangle(GameState state, Combatant victim, List<GameEvent> events)
    {
        if (victim.Kind == CombatantKind.Monster)
        {
            if (!state.HasMonster(victim.Id))
                return state;

            events.Add(new MonsterEntangled(victim.Id));
            return state.WithMonster(state.Monster(victim.Id) with { SkipsNextAction = true });
        }

        return state.Hunter(victim.Id).IsActive
            ? StatusRules.Apply(state, victim.Id, StatusKind.Trapped, 1, events)
            : state;
    }
}
