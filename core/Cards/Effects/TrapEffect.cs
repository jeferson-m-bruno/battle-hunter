using System.Collections.Generic;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>
/// "trap": coloca a armadilha na célula atual. O disparo (TrapRules) lê os params da carta:
/// `damage`, `net` (perde o próximo turno), `poison`, `alarm` (atrai monstros por `alarm` rodadas).
/// </summary>
public sealed class TrapEffect : ICardEffect
{
    public string Id => "trap";

    public string? Validate(EffectContext ctx)
    {
        if (ctx.State.TrapAt(ctx.Actor.Position) != null)
            return "Já há uma armadilha nesta célula.";

        if (ctx.State.Map[ctx.Actor.Position] == Cell.Exit)
            return "Não se arma armadilha na saída.";

        return null;
    }

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var trap = new Trap(ctx.Actor.Position, ctx.Card.Id, ctx.ActorId);
        events.Add(new TrapPlaced(ctx.ActorId, trap.Position, ctx.Card.Id));
        return ctx.State with { Traps = new List<Trap>(ctx.State.Traps) { trap } };
    }
}
