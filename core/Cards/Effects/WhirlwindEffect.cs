using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"whirlwind": um ataque normal em cada criatura adjacente ortogonal.</summary>
public sealed class WhirlwindEffect : ICardEffect
{
    public string Id => "whirlwind";

    public string? Validate(EffectContext ctx) =>
        Targets(ctx.State, ctx.Actor.Position).Any() ? null : "Não há ninguém adjacente.";

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var state = ctx.State;
        var attacker = Combatant.HunterRef(ctx.ActorId);

        foreach (var target in Targets(state, ctx.Actor.Position))
        {
            state = CombatRules.ResolveAttack(state, attacker, target, ctx.Random, ctx.Content, events);
            if (!state.Hunter(ctx.ActorId).IsActive)
                break;
        }

        return state;
    }

    private static List<Combatant> Targets(GameState state, Position center) =>
        Pathfinding.Neighbors(center)
            .Select(p => CombatRules.TargetAt(state, p))
            .Where(c => c != null)
            .Select(c => c!)
            .ToList();
}
