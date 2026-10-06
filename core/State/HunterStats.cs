using BattleHunter.Core.Cards;

namespace BattleHunter.Core.State;

/// <summary>Os 5 atributos do GDD. Equipamentos e nível os modificam.</summary>
public sealed record HunterStats(int MaxHp, int Attack, int Defense, int Speed, int Luck)
{
    /// <summary>Valores base de nível 1 (GDD, seção Caçadores).</summary>
    public static HunterStats Base => new(MaxHp: 20, Attack: 4, Defense: 2, Speed: 0, Luck: 1);

    public HunterStats Apply(StatMods mods) =>
        new(MaxHp + mods.Hp, Attack + mods.Atk, Defense + mods.Def, Speed + mods.Spd, Luck + mods.Luck);
}
