namespace BattleHunter.Core.Cards;

/// <summary>
/// Uma carta como descrita em data/cards.json. Cartas são dados, nunca código:
/// o comportamento ativo vem de <see cref="Effect"/>, resolvido no EffectRegistry.
/// </summary>
public sealed record Card(
    string Id,
    string Name,
    CardType Type,
    Rarity Rarity,
    int Cost,
    int Sell,
    StatMods Mods,
    string? Effect)
{
    public bool IsEquipment => Type is CardType.Weapon or CardType.Armor or CardType.Accessory;
}
