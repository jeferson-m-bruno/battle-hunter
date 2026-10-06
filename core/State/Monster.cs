namespace BattleHunter.Core.State;

/// <summary>Um monstro vivo no mapa. Os atributos fixos vêm do MonsterType em data/monsters.json.</summary>
public sealed record Monster(int Id, string TypeId, int Hp, Position Position);
