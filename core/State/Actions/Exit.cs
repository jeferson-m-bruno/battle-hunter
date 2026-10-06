namespace BattleHunter.Core.State.Actions;

/// <summary>Sai pela célula de saída por 1 PA; encerra a participação do caçador.</summary>
public sealed record Exit(int HunterId) : HunterAction(HunterId);
