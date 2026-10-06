namespace BattleHunter.Core.State.Actions;

/// <summary>
/// Usa uma carta da mão e a descarta: consumível (1 PA), armadilha na célula atual (2 PA)
/// ou ataque especial no lugar do ataque normal (2 PA). <paramref name="Target"/> é a célula-alvo quando o efeito pede.
/// </summary>
public sealed record UseCard(int HunterId, string CardId, Position? Target = null) : HunterAction(HunterId);
