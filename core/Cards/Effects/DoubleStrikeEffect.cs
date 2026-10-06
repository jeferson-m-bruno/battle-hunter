using System.Collections.Generic;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"double_strike": dois ataques normais seguidos no mesmo alvo (o segundo só se ele ainda estiver de pé).</summary>
public sealed class DoubleStrikeEffect : ICardEffect
{
    public string Id => "double_strike";

    public string? Validate(EffectContext ctx) => SpecialAttacks.ValidateAdjacentTarget(ctx);

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var attacker = Combatant.HunterRef(ctx.ActorId);
        var target = CombatRules.TargetAt(ctx.State, ctx.Target!)!;

        var state = CombatRules.ResolveAttack(ctx.State, attacker, target, ctx.Random, ctx.Content, events);
        if (SpecialAttacks.IsStanding(state, target))
            state = CombatRules.ResolveAttack(state, attacker, target, ctx.Random, ctx.Content, events);

        return state;
    }
}
