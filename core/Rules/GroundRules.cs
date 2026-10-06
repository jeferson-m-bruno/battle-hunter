using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Cartas no chão: pegar por 1 PA na célula atual.</summary>
internal static class GroundRules
{
    public const int PickUpCost = 1;

    public static ReducerResult PickUp(GameState state, State.Actions.PickUp action, IRandom random, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        if (state.ActionPoints < PickUpCost)
            return Reducer.Reject(state, action, "PA insuficiente para pegar a carta.");

        var hunter = state.Hunter(action.HunterId);
        var ground = state.GroundCards.FirstOrDefault(g => g.Position == hunter.Position && g.CardId == action.CardId);
        if (ground == null)
            return Reducer.Reject(state, action, "Não há essa carta no chão desta célula.");

        if (hunter.Hand.Count >= GameState.MaxHandSize)
            return Reducer.Reject(state, action, "Mão cheia: descarte uma carta antes.");

        var pointsLeft = state.ActionPoints - PickUpCost;
        var remaining = state.GroundCards.ToList();
        remaining.Remove(ground);

        var next = state with { GroundCards = remaining, ActionPoints = pointsLeft };
        var events = new List<GameEvent> { new CardPickedUp(hunter.Id, ground.CardId, pointsLeft) };
        next = ChestRules.GiveCard(next, hunter.Id, ground.CardId, events);

        if (pointsLeft == 0)
            next = TurnRules.EndTurn(next, random, content, events);

        return new ReducerResult(next, events);
    }
}
