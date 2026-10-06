using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Baús: 2 PA, adjacente ortogonal, uma carta sorteada pela tabela "chest" pesada pela SOR efetiva.</summary>
internal static class ChestRules
{
    public const int OpenCost = 2;

    public static ReducerResult Open(GameState state, OpenChest action, IRandom random, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        if (state.ActionPoints < OpenCost)
            return Reducer.Reject(state, action, "PA insuficiente para abrir o baú.");

        var hunter = state.Hunter(action.HunterId);
        if (!hunter.Position.IsOrthogonallyAdjacentTo(action.ChestPosition))
            return Reducer.Reject(state, action, "O baú precisa estar adjacente.");

        var chest = state.ChestAt(action.ChestPosition);
        if (chest == null)
            return Reducer.Reject(state, action, "Não há baú nessa célula.");

        if (chest.IsOpened)
            return Reducer.Reject(state, action, "O baú já foi aberto.");

        if (hunter.Hand.Count >= GameState.MaxHandSize)
            return Reducer.Reject(state, action, "Mão cheia: descarte uma carta antes.");

        var card = chest.HoldsTargetTreasure
            ? content.Cards.Get(state.TargetTreasureCardId!)
            : LootRoller.Roll(content.LootTable(GameContent.ChestLootTableId), content.Cards, StatRules.Effective(hunter, content).Luck, random);

        var pointsLeft = state.ActionPoints - OpenCost;
        var next = state
            .WithChest(chest with { IsOpened = true })
            .WithHunter(hunter.WithCardAdded(card.Id))
            with { ActionPoints = pointsLeft };

        var events = new List<GameEvent>
        {
            new ChestOpened(hunter.Id, chest.Position, pointsLeft),
            new CardDrawn(hunter.Id, card.Id),
        };

        if (chest.HoldsTargetTreasure)
            events.Add(new HunterMarked(hunter.Id));

        if (pointsLeft == 0)
            next = TurnRules.EndTurn(next, events);

        return new ReducerResult(next, events);
    }
}
