namespace BattleHunter.Core.State;

/// <summary>Parâmetros da missão que as regras leem. Os valores por tipo de missão vêm de data/missions.json (fatia 7).</summary>
public sealed record GameConfig(int MaxRounds = 30);
