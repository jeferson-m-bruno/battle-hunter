using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;

namespace BattleHunter.Server;

/// <summary>Tipos de missão aceitos na fila, vindos de data/missions.json.</summary>
public static class Missions
{
    public static IReadOnlyList<string> Ids(GameContent content) => content.Missions.All.Select(m => m.Id).ToList();

    public static bool IsValid(GameContent content, string id) => content.Missions.Contains(id);

    public static MatchSettings SettingsFor(GameContent content, string id) => MatchSettings.For(content.Missions.Get(id));
}
