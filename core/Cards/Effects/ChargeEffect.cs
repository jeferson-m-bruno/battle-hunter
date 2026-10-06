using System;
using System.Collections.Generic;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>
/// "charge": Investida — alvo em linha reta a 2..`range` células com o caminho livre; o atacante avança
/// até ficar adjacente (disparando armadilhas no caminho) e ataca com `bonus_atk`.
/// </summary>
public sealed class ChargeEffect : ICardEffect
{
    public string Id => "charge";

    public string? Validate(EffectContext ctx)
    {
        if (ctx.Target == null)
            return "Precisa de um alvo.";

        var from = ctx.Actor.Position;
        var to = ctx.Target;
        if (from.X != to.X && from.Y != to.Y)
            return "A investida só vai em linha reta.";

        var distance = from.DistanceTo(to);
        if (distance < 2 || distance > ctx.Param("range", 3))
            return "O alvo precisa estar a 2 ou mais células, dentro do alcance.";

        if (CombatRules.TargetAt(ctx.State, to) == null)
            return "Não há alvo nessa célula.";

        foreach (var cell in Path(from, to))
        {
            if (!ctx.State.Map.IsWalkable(cell) || ctx.State.IsOccupied(cell))
                return "O caminho até o alvo está bloqueado.";
        }

        return null;
    }

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var state = ctx.State;
        var target = CombatRules.TargetAt(state, ctx.Target!)!;

        foreach (var cell in Path(ctx.Actor.Position, ctx.Target!))
        {
            state = MovementRules.EnterCell(state, ctx.ActorId, cell, ctx.Random, ctx.Content, events);
            if (!state.Hunter(ctx.ActorId).IsActive)
                return state;
        }

        var mods = new AttackModifiers(BonusAttack: ctx.Param("bonus_atk"));
        return CombatRules.ResolveAttack(state, Combatant.HunterRef(ctx.ActorId), target, ctx.Random, ctx.Content, events, mods);
    }

    /// <summary>Células entre o atacante e o alvo, exclusive ambos.</summary>
    private static IEnumerable<Position> Path(Position from, Position to)
    {
        var dx = Math.Sign(to.X - from.X);
        var dy = Math.Sign(to.Y - from.Y);
        var p = new Position(from.X + dx, from.Y + dy);
        while (p != to)
        {
            yield return p;
            p = new Position(p.X + dx, p.Y + dy);
        }
    }
}
