using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Estados (GDD): Veneno tira 2 PV no início do turno por 3 turnos; Preso pula o próximo turno.</summary>
internal static class StatusRules
{
    public const int PoisonDamage = 2;

    public static GameState Apply(GameState state, int hunterId, StatusKind kind, int turns, List<GameEvent> events)
    {
        events.Add(new StatusApplied(hunterId, kind, turns));
        return state.WithHunter(state.Hunter(hunterId).WithStatus(kind, turns));
    }

    /// <summary>
    /// Roda no início do turno do caçador: tique do veneno e verificação de Preso.
    /// <paramref name="skipTurn"/> = o turno é pulado (caçador preso ou caído pelo veneno).
    /// </summary>
    public static GameState OnTurnStart(GameState state, int hunterId, IRandom random, GameContent content, List<GameEvent> events, out bool skipTurn)
    {
        skipTurn = false;
        var hunter = state.Hunter(hunterId);

        var poison = hunter.Statuses.FirstOrDefault(s => s.Kind == StatusKind.Poisoned);
        if (poison != null)
        {
            var self = Combatant.HunterRef(hunterId);
            state = CombatRules.ApplyDamage(state, Combatant.Environment, self, PoisonDamage, random, content, events,
                hpLeft => new DamageDealt(Combatant.Environment, self, PoisonDamage, hpLeft, "poison"));

            hunter = state.Hunter(hunterId);
            if (!hunter.IsActive)
            {
                skipTurn = true;
                return state;
            }

            hunter = poison.TurnsLeft <= 1 ? hunter.WithoutStatus(StatusKind.Poisoned) : hunter.WithStatus(StatusKind.Poisoned, 0) with
            {
                Statuses = hunter.Statuses.Select(s => s.Kind == StatusKind.Poisoned ? s with { TurnsLeft = s.TurnsLeft - 1 } : s).ToList(),
            };
            if (poison.TurnsLeft <= 1)
                events.Add(new StatusRemoved(hunterId, StatusKind.Poisoned));
            state = state.WithHunter(hunter);
        }

        if (hunter.HasStatus(StatusKind.Trapped))
        {
            state = state.WithHunter(hunter.WithoutStatus(StatusKind.Trapped));
            events.Add(new StatusRemoved(hunterId, StatusKind.Trapped));
            events.Add(new TurnSkipped(hunterId));
            skipTurn = true;
        }

        return state;
    }
}
