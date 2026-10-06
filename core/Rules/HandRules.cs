using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Mão e equipamento: descarte gratuito, equipar por 1 PA nos slots arma/armadura/acessório.</summary>
internal static class HandRules
{
    public const int EquipCost = 1;

    public static ReducerResult Discard(GameState state, State.Actions.Discard action)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        var hunter = state.Hunter(action.HunterId);
        if (!hunter.HasCard(action.CardId))
            return Reducer.Reject(state, action, "A carta não está na mão.");

        if (action.CardId == state.TargetTreasureCardId)
            return Reducer.Reject(state, action, "O tesouro-alvo não pode ser descartado.");

        var next = state.WithHunter(hunter.WithCardRemoved(action.CardId));
        return new ReducerResult(next, new GameEvent[] { new CardDiscarded(hunter.Id, action.CardId) });
    }

    public static ReducerResult Equip(GameState state, State.Actions.Equip action, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        if (state.ActionPoints < EquipCost)
            return Reducer.Reject(state, action, "PA insuficiente para equipar.");

        var hunter = state.Hunter(action.HunterId);
        if (!hunter.HasCard(action.CardId))
            return Reducer.Reject(state, action, "A carta não está na mão.");

        var card = content.Cards.Get(action.CardId);
        if (!card.IsEquipment)
            return Reducer.Reject(state, action, $"Carta do tipo {card.Type} não equipa.");

        var slot = Equipment.SlotFor(card.Type);
        var previous = hunter.Equipment[slot];

        var updated = hunter.WithCardRemoved(card.Id);
        if (previous != null)
            updated = updated.WithCardAdded(previous);
        updated = updated with { Equipment = hunter.Equipment.With(slot, card.Id) };

        var pointsLeft = state.ActionPoints - EquipCost;
        var next = state.WithHunter(updated) with { ActionPoints = pointsLeft };

        var events = new List<GameEvent>
        {
            new CardEquipped(hunter.Id, card.Id, slot, previous, pointsLeft),
        };

        if (pointsLeft == 0)
            next = TurnRules.EndTurn(next, events);

        return new ReducerResult(next, events);
    }
}
