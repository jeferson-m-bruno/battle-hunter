namespace BattleHunter.Core.State;

/// <summary>
/// Um baú do mapa. O conteúdo é sorteado ao abrir (depende da SOR de quem abre), exceto o do tesouro-alvo.
/// Um Mímico parece baú para todos até ser aberto.
/// </summary>
public sealed record Chest(Position Position, bool IsOpened, bool HoldsTargetTreasure, bool IsMimic = false);
