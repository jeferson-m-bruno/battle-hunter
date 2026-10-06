namespace BattleHunter.Core.State;

/// <summary>Armadilha colocada numa célula. Invisível aos outros; dispara em quem entra (menos o dono).</summary>
public sealed record Trap(Position Position, string CardId, int OwnerId);
