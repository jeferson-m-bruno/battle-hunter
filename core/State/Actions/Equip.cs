namespace BattleHunter.Core.State.Actions;

/// <summary>Equipa uma carta da mão no slot do seu tipo por 1 PA; a carta anterior do slot volta para a mão.</summary>
public sealed record Equip(int HunterId, string CardId) : HunterAction(HunterId);
