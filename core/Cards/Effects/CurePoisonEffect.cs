using System.Collections.Generic;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"cure_poison": remove Veneno.</summary>
public sealed class CurePoisonEffect : ICardEffect
{
    public string Id => "cure_poison";

    public string? Validate(EffectContext ctx) => null;

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var actor = ctx.Actor;
        if (!actor.HasStatus(StatusKind.Poisoned))
            return ctx.State;

        events.Add(new StatusRemoved(actor.Id, StatusKind.Poisoned));
        return ctx.State.WithHunter(actor.WithoutStatus(StatusKind.Poisoned));
    }
}
