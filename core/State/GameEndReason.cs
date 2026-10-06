namespace BattleHunter.Core.State;

public enum GameEndReason
{
    /// <summary>Um caçador saiu com o tesouro-alvo: venceu a missão.</summary>
    TreasureExtracted,
    /// <summary>Todos os caçadores saíram ou caíram.</summary>
    AllHuntersOut,
    /// <summary>Limite de rodadas: missão falha para todos.</summary>
    RoundLimit,
}
