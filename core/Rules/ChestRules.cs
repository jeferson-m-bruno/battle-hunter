using System;
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

        if (!chest.IsMimic && hunter.Hand.Count >= GameState.MaxHandSize)
            return Reducer.Reject(state, action, "Mão cheia: descarte uma carta antes.");

        var pointsLeft = state.ActionPoints - OpenCost;
        var next = state.WithChest(chest with { IsOpened = true }) with { ActionPoints = pointsLeft };
        var events = new List<GameEvent> { new ChestOpened(hunter.Id, chest.Position, pointsLeft) };

        if (chest.IsMimic)
        {
            // O "baú" era um Mímico: vira monstro na célula e ataca quem abriu na hora.
            next = MonsterRules.Spawn(next, content.Monsters.Get(MonsterCatalog.MimicId), chest.Position, events);
            var mimicId = next.NextMonsterId - 1;
            next = CombatRules.ResolveAttack(next, Combatant.MonsterRef(mimicId), Combatant.HunterRef(hunter.Id), random, content, events);
        }
        else if (!chest.HoldsTargetTreasure && state.Config.ChestGoldPercent > 0 && random.Next(100) < state.Config.ChestGoldPercent)
        {
            // Ouro solto (GDD): em vez de carta.
            var amount = state.Config.ChestGoldMin + random.Next(Math.Max(1, state.Config.ChestGoldMax - state.Config.ChestGoldMin + 1));
            var richer = next.Hunter(hunter.Id);
            next = next.WithHunter(richer with { Gold = richer.Gold + amount });
            events.Add(new GoldFound(hunter.Id, amount, richer.Gold + amount));
        }
        else
        {
            var card = chest.HoldsTargetTreasure
                ? content.Cards.Get(state.TargetTreasureCardId!)
                : LootRoller.Roll(content.LootTable(GameContent.ChestLootTableId), content.Cards, StatRules.Effective(hunter, content).Luck, random);

            next = GiveCard(next, hunter.Id, card.Id, random, content, events);
        }

        if (!chest.IsMimic && state.Config.ChestXp > 0)
        {
            var opener = next.Hunter(hunter.Id);
            next = next.WithHunter(opener with { Xp = opener.Xp + state.Config.ChestXp });
        }

        if (next.Phase == GamePhase.Finished)
            return new ReducerResult(next, events);

        if (pointsLeft == 0 || !next.Hunter(hunter.Id).IsActive)
            next = TurnRules.EndTurn(next, random, content, events);

        return new ReducerResult(next, events);
    }

    /// <summary>Entrega uma carta: na mão se há espaço, senão no chão da célula do caçador. O tesouro-alvo marca o caçador e chama o chefe.</summary>
    public static GameState GiveCard(GameState state, int hunterId, string cardId, IRandom random, GameContent content, List<GameEvent> events)
    {
        var hunter = state.Hunter(hunterId);
        events.Add(new CardDrawn(hunterId, cardId));

        if (hunter.Hand.Count >= GameState.MaxHandSize)
            return state.WithGroundCards(new[] { new GroundCard(hunter.Position, cardId) });

        state = state.WithHunter(hunter.WithCardAdded(cardId));
        if (cardId == state.TargetTreasureCardId)
        {
            events.Add(new HunterMarked(hunterId));
            state = BossRules.SpawnOnTreasure(state, random, content, events);
        }

        return state;
    }
}
