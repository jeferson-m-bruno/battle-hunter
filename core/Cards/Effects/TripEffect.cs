using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"trip": Rasteira — ataque normal; se acertar um caçador, `cards` cartas aleatórias da mão dele caem no chão.</summary>
public sealed class TripEffect : ICardEffect
{
    public string Id => "trip";

    public string? Validate(EffectContext ctx) => SpecialAttacks.ValidateAdjacentTarget(ctx);

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var target = CombatRules.TargetAt(ctx.State, ctx.Target!)!;
        var before = events.Count;
        var state = CombatRules.ResolveAttack(ctx.State, Combatant.HunterRef(ctx.ActorId), target, ctx.Random, ctx.Content, events);

        var hit = events.Skip(before).OfType<AttackResolved>().Any(a => a.Damage > 0);
        if (!hit || target.Kind != CombatantKind.Hunter || !state.Hunter(target.Id).IsActive)
            return state;

        var victim = state.Hunter(target.Id);
        var dropped = new List<string>();
        for (var i = 0; i < ctx.Param("cards", 2) && victim.Hand.Count > 0; i++)
        {
            var card = victim.Hand[ctx.Random.Next(victim.Hand.Count)];
            victim = victim.WithCardRemoved(card);
            dropped.Add(card);
        }

        if (dropped.Count == 0)
            return state;

        events.Add(new CardsDropped(victim.Id, victim.Position, dropped));
        return state
            .WithHunter(victim)
            .WithGroundCards(dropped.Select(c => new GroundCard(victim.Position, c)));
    }
}
