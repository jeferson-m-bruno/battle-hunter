using System.Collections.Generic;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Movimento: 1 célula ortogonal por 1 PA. Paredes e outros caçadores bloqueiam.</summary>
internal static class MovementRules
{
    public const int MoveCost = 1;

    public static ReducerResult Move(GameState state, MoveTo action)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        if (state.ActionPoints < MoveCost)
            return Reducer.Reject(state, action, "PA insuficiente para mover.");

        var hunter = state.Hunter(action.HunterId);

        if (!hunter.Position.IsOrthogonallyAdjacentTo(action.Target))
            return Reducer.Reject(state, action, "Só é possível mover 1 célula ortogonal.");

        if (!state.Map.IsWalkable(action.Target))
            return Reducer.Reject(state, action, "Célula bloqueada ou fora do mapa.");

        if (state.IsOccupied(action.Target))
            return Reducer.Reject(state, action, "Célula ocupada por outro caçador.");

        var pointsLeft = state.ActionPoints - MoveCost;
        var next = state
            .WithHunter(hunter with { Position = action.Target })
            with { ActionPoints = pointsLeft };

        var events = new List<GameEvent>
        {
            new HunterMoved(hunter.Id, hunter.Position, action.Target, pointsLeft),
        };

        if (pointsLeft == 0)
            next = TurnRules.EndTurn(next, events);

        return new ReducerResult(next, events);
    }
}
