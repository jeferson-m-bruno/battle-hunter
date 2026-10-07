using System;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Progression;

/// <summary>
/// Progressão (GDD, seção Caçadores): ganhos automáticos por nível (+3 PV, +1 ATQ, +1 DEF a cada 2,
/// +1 VEL a cada 3, +1 SOR a cada 2) mais 1 ponto livre por nível.
/// </summary>
public static class LevelRules
{
    public enum Stat { Hp, Atk, Def, Spd, Luck }

    /// <summary>Atributos base do caçador no nível, sem equipamento (o equipamento entra em StatRules.Effective).</summary>
    public static HunterStats StatsFor(Profile profile)
    {
        var n = Math.Clamp(profile.Level, 1, LevelTable.MaxLevel) - 1;
        var b = HunterStats.Base;
        var p = profile.Points;
        return new HunterStats(
            MaxHp: b.MaxHp + 3 * n + p.Hp,
            Attack: b.Attack + n + p.Atk,
            Defense: b.Defense + n / 2 + p.Def,
            Speed: b.Speed + n / 3 + p.Spd,
            Luck: b.Luck + n / 2 + p.Luck);
    }

    /// <summary>Credita XP; sobe os níveis que couberem e dá 1 ponto livre por nível.</summary>
    public static (Profile Profile, int LevelsGained) GrantXp(Profile profile, int xp, LevelTable table)
    {
        if (xp <= 0)
            return (profile, 0);

        var total = profile.Xp + xp;
        var level = table.LevelFor(total);
        var gained = Math.Max(0, level - profile.Level);
        return (profile with { Xp = total, Level = Math.Max(level, profile.Level), UnspentPoints = profile.UnspentPoints + gained }, gained);
    }

    /// <summary>Gasta 1 ponto livre num atributo. Null se não há ponto.</summary>
    public static Profile? AllocatePoint(Profile profile, Stat stat)
    {
        if (profile.UnspentPoints <= 0)
            return null;

        var p = profile.Points;
        p = stat switch
        {
            Stat.Hp => p with { Hp = p.Hp + 1 },
            Stat.Atk => p with { Atk = p.Atk + 1 },
            Stat.Def => p with { Def = p.Def + 1 },
            Stat.Spd => p with { Spd = p.Spd + 1 },
            _ => p with { Luck = p.Luck + 1 },
        };
        return profile with { Points = p, UnspentPoints = profile.UnspentPoints - 1 };
    }

    public static int XpToNextLevel(Profile profile, LevelTable table) =>
        profile.Level >= table.Count ? 0 : Math.Max(0, table.XpFor(profile.Level + 1) - profile.Xp);
}
