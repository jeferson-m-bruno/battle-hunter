using System;
using System.Collections.Generic;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"heal": recupera `amount` PV, até o máximo efetivo.</summary>
public sealed class HealEffect : ICardEffect
{
    public string Id => "heal";

    public string? Validate(EffectContext ctx) => null;

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var actor = ctx.Actor;
        var max = StatRules.Effective(actor, ctx.Content).MaxHp;
        var hp = Math.Min(max, actor.Hp + ctx.Param("amount"));
        events.Add(new HunterHealed(actor.Id, hp - actor.Hp, hp));
        return ctx.State.WithHunter(actor with { Hp = hp });
    }
}
