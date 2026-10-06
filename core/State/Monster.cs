namespace BattleHunter.Core.State;

/// <summary>Um monstro vivo no mapa. Os atributos fixos vêm do MonsterType em data/monsters.json.</summary>
/// <param name="SkipsNextAction">Preso numa Rede: não age na próxima fase dos monstros.</param>
public sealed record Monster(int Id, string TypeId, int Hp, Position Position, bool SkipsNextAction = false);
