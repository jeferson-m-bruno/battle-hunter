using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>Validações comuns aos ataques especiais.</summary>
internal static class SpecialAttacks
{
    public static string? ValidateAdjacentTarget(EffectContext ctx)
    {
        if (ctx.Target == null)
            return "Precisa de um alvo.";

        if (!ctx.Actor.Position.IsOrthogonallyAdjacentTo(ctx.Target))
            return "O alvo precisa estar adjacente.";

        if (CombatRules.TargetAt(ctx.State, ctx.Target) == null)
            return "Não há alvo nessa célula.";

        return null;
    }

    public static bool IsStanding(GameState state, Combatant c) =>
        c.Kind == CombatantKind.Monster ? state.HasMonster(c.Id) : state.Hunter(c.Id).IsActive;
}
