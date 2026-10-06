namespace BattleHunter.Core.State;

public enum GameEndReason
{
    /// <summary>Todos os caçadores saíram ou caíram.</summary>
    AllHuntersOut,
    /// <summary>Limite de rodadas: missão falha para todos.</summary>
    RoundLimit,
}
