using System;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Progression;

/// <summary>Ranqueada: +20 por vitória, +5 por sair vivo, −10 por cair, nunca abaixo de 0. Temporada = mês.</summary>
public static class RankRules
{
    public const int WinPoints = 20;
    public const int ExitPoints = 5;
    public const int FallPoints = -10;

    public static string SeasonOf(DateTimeOffset now) => now.ToString("yyyy-MM");

    public static int Delta(bool won, HunterStatus status) =>
        won ? WinPoints : status == HunterStatus.Exited ? ExitPoints : status == HunterStatus.Fallen ? FallPoints : 0;

    /// <summary>Zera o rank ao entrar numa temporada nova.</summary>
    public static Profile EnsureSeason(Profile profile, string season) =>
        profile.Season == season ? profile : profile with { Season = season, RankPoints = 0 };
}
