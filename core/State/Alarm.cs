namespace BattleHunter.Core.State;

/// <summary>Alarme ativo: monstros sem alvo à vista vão até a célula enquanto durar.</summary>
public sealed record Alarm(Position Position, int RoundsLeft);
