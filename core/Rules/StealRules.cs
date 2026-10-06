using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>
/// Roubo (GDD): após dano de caçador em caçador, se o alvo falha num teste de SOR
/// (1d6 + SOR do alvo &lt; 1d6 + SOR do atacante), o atacante rouba 1 carta aleatória da mão.
/// O tesouro-alvo é sempre roubável e tem prioridade.
/// </summary>
internal static class StealRules
{
    public static GameState TryStealAfterHit(GameState state, int thiefId, int victimId, IRandom random, GameContent content, List<GameEvent> events)
    {
        var victim = state.Hunter(victimId);
        if (!victim.IsActive || victim.Hand.Count == 0)
            return state;

        var thief = state.Hunter(thiefId);
        var victimRoll = random.NextD6() + StatRules.Effective(victim, content).Luck;
        var thiefRoll = random.NextD6() + StatRules.Effective(thief, content).Luck;
        if (victimRoll >= thiefRoll)
            return state;

        var cardId = state.TargetTreasureCardId != null && victim.HasCard(state.TargetTreasureCardId)
            ? state.TargetTreasureCardId
            : victim.Hand[random.Next(victim.Hand.Count)];

        state = state.WithHunter(victim.WithCardRemoved(cardId));
        events.Add(new CardStolen(thiefId, victimId, cardId));
        return ChestRules.GiveCard(state, thiefId, cardId, random, content, events);
    }
}
