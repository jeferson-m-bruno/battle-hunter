namespace BattleHunter.Core.Cards;

/// <summary>Uma linha de data/missions.json (GDD, tabela de tipos de missão).</summary>
public sealed record MissionType(
    string Id,
    string Name,
    int GridSize,
    int MaxRounds,
    bool Boss,
    int BossHpPercent,
    int RewardGold,
    int RewardXp,
    bool RewardRareCard,
    bool Ranked,
    int RequiredLevel);
