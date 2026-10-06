namespace BattleHunter.Core.State.Actions;

/// <summary>Abre um baú adjacente por 2 PA; a carta sorteada vai para a mão.</summary>
public sealed record OpenChest(int HunterId, Position ChestPosition) : HunterAction(HunterId);
