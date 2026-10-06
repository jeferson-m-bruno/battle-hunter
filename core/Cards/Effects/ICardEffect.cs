namespace BattleHunter.Core.Cards.Effects;

/// <summary>
/// Efeito ativo de uma carta, identificado pelo campo "effect" do JSON.
/// Cada efeito é uma função pura sobre o estado; a assinatura de execução entra na fatia 3
/// junto com as primeiras cartas que o usam.
/// </summary>
public interface ICardEffect
{
    string Id { get; }
}
