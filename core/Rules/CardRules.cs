using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Cards.Effects;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Uso de cartas ativas: valida, paga o PA, descarta e delega ao efeito registrado.</summary>
internal static class CardRules
{
    public const int ConsumableCost = 1;
    public const int TrapCost = 2;
    public const int SpecialAttackCost = 2;

    public static ReducerResult Use(GameState state, UseCard action, IRandom random, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        var hunter = state.Hunter(action.HunterId);
        if (!hunter.HasCard(action.CardId))
            return Reducer.Reject(state, action, "A carta não está na mão.");

        var card = content.Cards.Get(action.CardId);
        if (!card.IsUsable || card.Effect == null)
            return Reducer.Reject(state, action, $"Carta do tipo {card.Type} não se usa; equipe ou carregue.");

        var cost = CostOf(card.Type);
        if (state.ActionPoints < cost)
            return Reducer.Reject(state, action, "PA insuficiente para usar a carta.");

        var effect = content.Effects.Get(card.Effect);
        var ctx = new EffectContext(state, hunter.Id, card, action.Target, random, content);
        var reason = effect.Validate(ctx);
        if (reason != null)
            return Reducer.Reject(state, action, reason);

        var pointsLeft = state.ActionPoints - cost;
        var next = state.WithHunter(hunter.WithCardRemoved(card.Id)) with { ActionPoints = pointsLeft };
        var events = new List<GameEvent> { new CardUsed(hunter.Id, card.Id, action.Target, pointsLeft) };

        next = effect.Apply(ctx with { State = next }, events);

        if (next.Phase == GamePhase.Finished)
            return new ReducerResult(next, events);

        if (next.ActionPoints == 0 || !next.Hunter(hunter.Id).IsActive)
            next = TurnRules.EndTurn(next, random, content, events);

        return new ReducerResult(next, events);
    }

    public static int CostOf(CardType type) => type switch
    {
        CardType.Consumable => ConsumableCost,
        CardType.Trap => TrapCost,
        _ => SpecialAttackCost,
    };
}
