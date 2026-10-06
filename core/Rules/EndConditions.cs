using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Saída pela célula de saída. As demais condições de fim ficam em TurnRules.EndTurn.</summary>
internal static class EndConditions
{
    public const int ExitCost = 1;

    public static ReducerResult Exit(GameState state, State.Actions.Exit action, IRandom random, GameContent content)
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

        next = TurnRules.EndTurn(next, random, content, events);
        return new ReducerResult(next, events);
    }
}
