namespace BattleHunter.Core.State;

public enum GamePhase
{
    NotStarted,
    /// <summary>Caçador atual precisa rolar o dado.</summary>
    AwaitingRoll,
    /// <summary>Caçador atual gasta PA em ações.</summary>
    Acting,
    Finished,
}
