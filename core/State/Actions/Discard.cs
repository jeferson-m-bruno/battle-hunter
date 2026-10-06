namespace BattleHunter.Core.State.Actions;

/// <summary>Descarta uma carta da mão. Gratuito (0 PA); obrigatório antes de pegar a 11ª carta.</summary>
public sealed record Discard(int HunterId, string CardId) : HunterAction(HunterId);
