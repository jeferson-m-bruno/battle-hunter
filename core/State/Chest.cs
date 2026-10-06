namespace BattleHunter.Core.State;

/// <summary>Um baú do mapa. O conteúdo é sorteado ao abrir (depende da SOR de quem abre), exceto o do tesouro-alvo.</summary>
public sealed record Chest(Position Position, bool IsOpened, bool HoldsTargetTreasure);
