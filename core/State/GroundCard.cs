namespace BattleHunter.Core.State;

/// <summary>Carta largada no chão (mão de um caçador que caiu, ou loot sem espaço na mão). Pegável por 1 PA.</summary>
public sealed record GroundCard(Position Position, string CardId);
