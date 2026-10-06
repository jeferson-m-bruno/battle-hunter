namespace BattleHunter.Core.State.Actions;

/// <summary>Move 1 célula ortogonal por 1 PA.</summary>
public sealed record MoveTo(int HunterId, Position Target) : HunterAction(HunterId);
