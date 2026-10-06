using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>
/// Ponto único de entrada das regras: Apply(state, action, random, content) -> (state, events).
/// Função pura: nunca muda o estado recebido. Ação inválida devolve o mesmo estado
/// e um único evento ActionRejected.
/// </summary>
public static class Reducer
{
    public static ReducerResult Apply(GameState state, GameAction action, IRandom random, GameContent content)
    {
        if (state.Phase == GamePhase.Finished)
            return Reject(state, action, "A partida já terminou.");

        return action switch
        {
            StartGame start => TurnRules.Start(state, start, random),
            RollDice roll => TurnRules.Roll(state, roll, random, content),
            Pass pass => TurnRules.Pass(state, pass, random, content),
            MoveTo move => MovementRules.Move(state, move, random, content),
            Exit exit => EndConditions.Exit(state, exit, random, content),
            OpenChest open => ChestRules.Open(state, open, random, content),
            Discard discard => HandRules.Discard(state, discard),
            Equip equip => HandRules.Equip(state, equip, random, content),
            Attack attack => CombatRules.Attack(state, attack, random, content),
            PickUp pickUp => GroundRules.PickUp(state, pickUp, random, content),
            _ => Reject(state, action, $"Ação desconhecida: {action.GetType().Name}."),
        };
    }

    internal static ReducerResult Reject(GameState state, GameAction action, string reason) =>
        new(state, new GameEvent[] { new ActionRejected(action, reason) });

    /// <summary>Verifica que é a vez do caçador e que a partida está na fase esperada.</summary>
    internal static string? CheckTurn(GameState state, HunterAction action, GamePhase expected)
    {
        if (state.Phase != expected)
            return $"Fase atual é {state.Phase}; esperado {expected}.";

        if (state.CurrentHunterId != action.HunterId)
            return $"Não é a vez do caçador {action.HunterId}.";

        return null;
    }
}
