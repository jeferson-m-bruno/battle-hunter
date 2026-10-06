namespace BattleHunter.Core.State.Actions;

/// <summary>Ataca um alvo adjacente ortogonal (monstro ou caçador) por 2 PA.</summary>
public sealed record Attack(int HunterId, Position Target) : HunterAction(HunterId);
