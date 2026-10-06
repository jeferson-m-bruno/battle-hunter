namespace BattleHunter.Core.Cards;

/// <summary>Modificadores de atributo de uma carta equipada. Chaves no JSON: hp, atk, def, spd, luck.</summary>
public sealed record StatMods(int Hp = 0, int Atk = 0, int Def = 0, int Spd = 0, int Luck = 0)
{
    public static StatMods None => new();

    public StatMods Add(StatMods other) =>
        new(Hp + other.Hp, Atk + other.Atk, Def + other.Def, Spd + other.Spd, Luck + other.Luck);
}
