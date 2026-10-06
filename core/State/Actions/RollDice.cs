namespace BattleHunter.Core.State.Actions;

/// <summary>Rola 1d6 + VEL e credita como PA do turno.</summary>
public sealed record RollDice(int HunterId) : HunterAction(HunterId);
