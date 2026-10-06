using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"bomb": `damage` fixo a todos (caçadores e monstros, inclusive quem joga) numa área 3×3 ao redor de uma célula a até `range` passos.</summary>
public sealed class BombEffect : ICardEffect
{
    public string Id => "bomb";

    public string? Validate(EffectContext ctx)
    {
        if (ctx.Target == null)
            return "A bomba precisa de uma célula-alvo.";

        if (!ctx.State.Map.IsInside(ctx.Target))
            return "Célula-alvo fora do mapa.";

        if (ctx.Actor.Position.DistanceTo(ctx.Target) > ctx.Param("range", 3))
            return "Célula-alvo longe demais.";

        return null;
    }

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var state = ctx.State;
        var source = Combatant.HunterRef(ctx.ActorId);
        var damage = ctx.Param("damage");
        var target = ctx.Target!;

        bool InArea(Position p) => Math.Abs(p.X - target.X) <= 1 && Math.Abs(p.Y - target.Y) <= 1;

        var victims = state.Hunters.Where(h => h.IsActive && InArea(h.Position)).Select(h => Combatant.HunterRef(h.Id))
            .Concat(state.Monsters.Where(m => InArea(m.Position)).Select(m => Combatant.MonsterRef(m.Id)))
            .ToList();

        foreach (var victim in victims)
        {
            state = CombatRules.ApplyDamage(state, source, victim, damage, ctx.Random, ctx.Content, events,
                hpLeft => new DamageDealt(source, victim, damage, hpLeft, ctx.Card.Id));
        }

        return state;
    }
}
