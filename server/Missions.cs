using BattleHunter.Core.Ai;

namespace BattleHunter.Server;

/// <summary>Tipos de missão aceitos na fila. Os valores completos (recompensas, requisitos) vêm de data/missions.json na fatia 7.</summary>
public static class Missions
{
    public static readonly IReadOnlyList<string> Ids = new[] { "easy", "normal", "hard", "ranked" };

    public static bool IsValid(string id) => Ids.Contains(id, StringComparer.Ordinal);

    public static MatchSettings SettingsFor(string id) => id switch
    {
        "normal" => MatchSettings.Normal,
        "hard" => MatchSettings.Hard,
        "ranked" => MatchSettings.Normal,
        _ => MatchSettings.Easy,
    };
}
