using System.Collections.Generic;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"gain_ap": +`amount` PA neste turno.</summary>
public sealed class GainActionPointsEffect : ICardEffect
{
    public string Id => "gain_ap";

    public string? Validate(EffectContext ctx) => null;

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var points = ctx.State.ActionPoints + ctx.Param("amount");
        events.Add(new ActionPointsGained(ctx.ActorId, ctx.Param("amount"), points));
        return ctx.State with { ActionPoints = points };
    }
}
