namespace BattleHunter.Core.State.Actions;

/// <summary>Pega uma carta do chão da célula atual por 1 PA.</summary>
public sealed record PickUp(int HunterId, string CardId) : HunterAction(HunterId);
