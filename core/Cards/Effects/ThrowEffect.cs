using System.Collections.Generic;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"throw": `damage` fixo a um alvo único a até `range` passos (sem dados, sem esquiva).</summary>
public sealed class ThrowEffect : ICardEffect
{
    public string Id => "throw";

    public string? Validate(EffectContext ctx)
    {
        if (ctx.Target == null)
            return "Precisa de um alvo.";

        if (ctx.Actor.Position.DistanceTo(ctx.Target) > ctx.Param("range", 3))
            return "Alvo longe demais.";

        if (ctx.Target == ctx.Actor.Position || CombatRules.TargetAt(ctx.State, ctx.Target) == null)
            return "Não há alvo nessa célula.";

        return null;
    }

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var source = Combatant.HunterRef(ctx.ActorId);
        var target = CombatRules.TargetAt(ctx.State, ctx.Target!)!;
        var damage = ctx.Param("damage");
        return CombatRules.ApplyDamage(ctx.State, source, target, damage, ctx.Random, ctx.Content, events,
            hpLeft => new DamageDealt(source, target, damage, hpLeft, ctx.Card.Id));
    }
}
