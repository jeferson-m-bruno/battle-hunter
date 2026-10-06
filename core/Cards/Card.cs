using System.Collections.Generic;

namespace BattleHunter.Core.Cards;

/// <summary>
/// Uma carta como descrita em data/cards.json. Cartas são dados, nunca código:
/// o comportamento ativo vem de <see cref="Effect"/> (resolvido no EffectRegistry) e dos <see cref="Params"/>.
/// </summary>
public sealed record Card(
    string Id,
    string Name,
    CardType Type,
    Rarity Rarity,
    int Cost,
    int Sell,
    StatMods Mods,
    string? Effect,
    IReadOnlyDictionary<string, int> Params)
{
    public bool IsEquipment => Type is CardType.Weapon or CardType.Armor or CardType.Accessory;

    public bool IsUsable => Type is CardType.Consumable or CardType.Trap or CardType.SpecialAttack;

    public int Param(string key, int fallback = 0) => Params.TryGetValue(key, out var v) ? v : fallback;
}
