namespace BattleHunter.Core.State;

public sealed record Hunter(
    int Id,
    string Name,
    HunterStats Stats,
    int Hp,
    Position Position,
    HunterStatus Status)
{
    public static Hunter Create(int id, string name, HunterStats stats, Position position) =>
        new(id, name, stats, stats.MaxHp, position, HunterStatus.Active);

    public bool IsActive => Status == HunterStatus.Active;
}
