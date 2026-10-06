namespace BattleHunter.Core.State.Actions;

/// <summary>Encerra o turno; PA não gasto é perdido.</summary>
public sealed record Pass(int HunterId) : HunterAction(HunterId);
