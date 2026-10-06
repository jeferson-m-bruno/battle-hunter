using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Saída pela célula de saída e condições de fim de partida.</summary>
internal static class EndConditions
{
    public const int ExitCost = 1;

    public static ReducerResult Exit(GameState state, State.Actions.Exit action)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        if (state.ActionPoints < ExitCost)
            return Reducer.Reject(state, action, "PA insuficiente para sair.");

        var hunter = state.Hunter(action.HunterId);
        if (state.Map[hunter.Position] != Cell.Exit)
            return Reducer.Reject(state, action, "O caçador não está na célula de saída.");

        var next = state
            .WithHunter(hunter with { Status = HunterStatus.Exited })
            with { ActionPoints = state.ActionPoints - ExitCost };

        var events = new List<GameEvent> { new HunterExited(hunter.Id) };

        // Sair com o tesouro-alvo vence a missão e encerra a partida para todos.
        if (state.IsMarked(hunter.Id))
            return new ReducerResult(TurnRules.Finish(next, GameEndReason.TreasureExtracted, hunter.Id, events), events);

        next = TurnRules.EndTurn(next, events);
        return new ReducerResult(next, events);
    }

    /// <summary>Motivo de fim avaliado ao encerrar um turno; null se a partida continua.</summary>
    public static GameEndReason? CheckAfterTurn(GameState state)
    {
        if (!state.Hunters.Any(h => h.IsActive))
            return GameEndReason.AllHuntersOut;

        return null;
    }
}
