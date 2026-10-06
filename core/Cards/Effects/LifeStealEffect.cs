using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"life_steal": ataque normal; o atacante recupera metade do dano causado (arredondado para baixo).</summary>
public sealed class LifeStealEffect : ICardEffect
{
    public string Id => "life_steal";

    public string? Validate(EffectContext ctx) => SpecialAttacks.ValidateAdjacentTarget(ctx);

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var target = CombatRules.TargetAt(ctx.State, ctx.Target!)!;
        var before = events.Count;
        var state = CombatRules.ResolveAttack(ctx.State, Combatant.HunterRef(ctx.ActorId), target, ctx.Random, ctx.Content, events);

        var damage = events.Skip(before).OfType<AttackResolved>().Sum(a => a.Damage);
        var actor = state.Hunter(ctx.ActorId);
        if (damage < 2 || !actor.IsActive)
            return state;

        var max = StatRules.Effective(actor, ctx.Content).MaxHp;
        var hp = Math.Min(max, actor.Hp + damage / 2);
        events.Add(new HunterHealed(actor.Id, hp - actor.Hp, hp));
        return state.WithHunter(actor with { Hp = hp });
    }
}
