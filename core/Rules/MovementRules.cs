using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Movimento: 1 célula ortogonal por 1 PA. Paredes, baús e outras criaturas bloqueiam.</summary>
internal static class MovementRules
{
    public const int MoveCost = 1;

    public static ReducerResult Move(GameState state, MoveTo action, IRandom random, GameContent content)
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
            return Reducer.Reject(state, action, "Célula ocupada por outra criatura.");

        var pointsLeft = state.ActionPoints - MoveCost;
        var events = new List<GameEvent>();
        var next = EnterCell(state with { ActionPoints = pointsLeft }, hunter.Id, action.Target, random, content, events);

        if (next.Phase == GamePhase.Finished)
            return new ReducerResult(next, events);

        if (pointsLeft == 0 || !next.Hunter(hunter.Id).IsActive)
            next = TurnRules.EndTurn(next, random, content, events);

        return new ReducerResult(next, events);
    }

    /// <summary>Move o caçador para a célula (sem validar) e dispara a armadilha que houver nela.</summary>
    public static GameState EnterCell(GameState state, int hunterId, Position to, IRandom random, GameContent content, List<GameEvent> events)
    {
        var hunter = state.Hunter(hunterId);
        events.Add(new HunterMoved(hunterId, hunter.Position, to, state.ActionPoints));
        state = state.WithHunter(hunter with { Position = to });
        return TrapRules.TriggerIfAny(state, Combatant.HunterRef(hunterId), to, random, content, events);
    }
}
