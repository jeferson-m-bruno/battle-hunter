using System.Collections.Generic;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>
/// Efeito ativo de uma carta, identificado pelo campo "effect" do JSON; os números vêm de "params".
/// Cada efeito é uma função pura: valida o contexto e devolve um novo estado mais eventos.
/// </summary>
public interface ICardEffect
{
    string Id { get; }

    /// <summary>Motivo da recusa, ou null se a carta pode ser usada assim.</summary>
    string? Validate(EffectContext ctx);

    GameState Apply(EffectContext ctx, List<GameEvent> events);
}

/// <summary>Tudo que um efeito precisa: quem usa, a carta, a célula-alvo (se houver), o dado e o conteúdo.</summary>
public sealed record EffectContext(
    GameState State,
    int ActorId,
    Card Card,
    Position? Target,
    IRandom Random,
    GameContent Content)
{
    public Hunter Actor => State.Hunter(ActorId);

    public int Param(string key, int fallback = 0) => Card.Param(key, fallback);
}
