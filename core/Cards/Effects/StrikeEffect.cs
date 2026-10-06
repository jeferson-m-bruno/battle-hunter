using System.Collections.Generic;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>"strike": ataque normal com `bonus_atk` de bônus; `no_dodge` = 1 impede a esquiva.</summary>
public sealed class StrikeEffect : ICardEffect
{
    public string Id => "strike";

    public string? Validate(EffectContext ctx) => SpecialAttacks.ValidateAdjacentTarget(ctx);

    public GameState Apply(EffectContext ctx, List<GameEvent> events)
    {
        var target = CombatRules.TargetAt(ctx.State, ctx.Target!)!;
        var mods = new AttackModifiers(BonusAttack: ctx.Param("bonus_atk"), NoDodge: ctx.Param("no_dodge") == 1);
        return CombatRules.ResolveAttack(ctx.State, Combatant.HunterRef(ctx.ActorId), target, ctx.Random, ctx.Content, events, mods);
    }
}
