namespace BattleHunter.Core.State;

public enum CombatantKind
{
    Hunter,
    Monster,
}

/// <summary>Referência a quem ataca ou é atacado: um caçador (id de caçador) ou um monstro (id de monstro).</summary>
public sealed record Combatant(CombatantKind Kind, int Id)
{
    public static Combatant HunterRef(int id) => new(CombatantKind.Hunter, id);
    public static Combatant MonsterRef(int id) => new(CombatantKind.Monster, id);
}
