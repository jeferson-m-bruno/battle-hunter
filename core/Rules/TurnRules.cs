using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Início de partida, dado do turno, passagem e avanço da ordem de turno.</summary>
internal static class TurnRules
{
    public static ReducerResult Start(GameState state, StartGame action, IRandom random)
    {
        if (state.Phase != GamePhase.NotStarted)
            return Reducer.Reject(state, action, "A partida já começou.");

        var order = random.Shuffle(state.Hunters.Select(h => h.Id).ToList());
        var started = state with
        {
            TurnOrder = order,
            TurnIndex = 0,
            Round = 1,
            ActionPoints = 0,
            Phase = GamePhase.AwaitingRoll,
        };

        return new ReducerResult(started, new GameEvent[]
        {
            new GameStarted(order),
            new TurnStarted(started.CurrentHunterId, started.Round),
        });
    }

    public static ReducerResult Roll(GameState state, RollDice action, IRandom random, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.AwaitingRoll);
        if (error != null)
            return Reducer.Reject(state, action, error);

        var hunter = state.Hunter(action.HunterId);
        var speed = StatRules.Effective(hunter, content).Speed;
        var die = random.NextD6();
        var points = die + speed;

        var next = state with { ActionPoints = points, Phase = GamePhase.Acting };
        return new ReducerResult(next, new GameEvent[]
        {
            new DiceRolled(hunter.Id, die, speed, points),
        });
    }

    public static ReducerResult Pass(GameState state, State.Actions.Pass action, IRandom random, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        var events = new List<GameEvent>();
        var next = EndTurn(state, random, content, events);
        return new ReducerResult(next, events);
    }

    /// <summary>
    /// Encerra o turno atual e abre o próximo, pulando caçadores que saíram ou caíram.
    /// Ao dar a volta na ordem: fase dos monstros, nova rodada, spawn periódico. Verifica as condições de fim.
    /// </summary>
    public static GameState EndTurn(GameState state, IRandom random, GameContent content, List<GameEvent> events)
    {
        events.Add(new TurnEnded(state.CurrentHunterId));

        if (!AnyHunterActive(state))
            return Finish(state, GameEndReason.AllHuntersOut, winnerId: null, events);

        var index = NextActiveIndex(state, state.TurnIndex);
        var round = state.Round;

        if (index == null)
        {
            // Todos os caçadores desta rodada já agiram: vez dos monstros, depois a rodada vira.
            state = MonsterRules.Phase(state, random, content, events);
            if (!AnyHunterActive(state))
                return Finish(state, GameEndReason.AllHuntersOut, winnerId: null, events);

            round++;
            if (round > state.Config.MaxRounds)
                return Finish(state, GameEndReason.RoundLimit, winnerId: null, events);

            state = state with { Round = round };
            state = MonsterRules.SpawnIfDue(state, random, content, events);
            index = NextActiveIndex(state, -1);
        }

        var next = state with
        {
            TurnIndex = index!.Value,
            Round = round,
            ActionPoints = 0,
            Phase = GamePhase.AwaitingRoll,
        };
        events.Add(new TurnStarted(next.CurrentHunterId, next.Round));
        return next;
    }

    public static GameState Finish(GameState state, GameEndReason reason, int? winnerId, List<GameEvent> events)
    {
        events.Add(new GameEnded(reason, winnerId));
        return state with { ActionPoints = 0, Phase = GamePhase.Finished, EndReason = reason, WinnerId = winnerId };
    }

    private static bool AnyHunterActive(GameState state) => state.Hunters.Any(h => h.IsActive);

    /// <summary>Próximo índice da ordem de turno com caçador ativo, depois de <paramref name="after"/>; null se a rodada acabou.</summary>
    private static int? NextActiveIndex(GameState state, int after)
    {
        for (var i = after + 1; i < state.TurnOrder.Count; i++)
        {
            if (state.Hunter(state.TurnOrder[i]).IsActive)
                return i;
        }

        return null;
    }
}
