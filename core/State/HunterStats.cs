namespace BattleHunter.Core.State;

/// <summary>Os 5 atributos do GDD. Equipamentos e nível os modificam (fatias 2 e 7).</summary>
public sealed record HunterStats(int MaxHp, int Attack, int Defense, int Speed, int Luck)
{
    /// <summary>Valores base de nível 1 (GDD, seção Caçadores).</summary>
    public static HunterStats Base => new(MaxHp: 20, Attack: 4, Defense: 2, Speed: 0, Luck: 1);
}
