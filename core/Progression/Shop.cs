using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Rules;

namespace BattleHunter.Core.Progression;

/// <summary>
/// Loja da guilda (GDD): estoque rotativo diário de comuns e incomuns (raras só em partida);
/// compra = 3× o valor de venda; venda de cartas do inventário pelo valor de venda; depósito de ouro.
/// </summary>
public static class Shop
{
    public const int PriceMultiplier = 3;
    public const int CommonSlots = 6;
    public const int UncommonSlots = 3;

    /// <summary>Estoque do dia, igual para todos: sorteado com seed = dia.</summary>
    public static IReadOnlyList<string> StockFor(DateTimeOffset day, CardCatalog cards)
    {
        var random = new SeededRandom(day.Year * 10_000 + day.Month * 100 + day.Day);
        var commons = Pool(cards, Rarity.Common);
        var uncommons = Pool(cards, Rarity.Uncommon);
        return random.Shuffle(commons).Take(CommonSlots).Concat(random.Shuffle(uncommons).Take(UncommonSlots)).ToList();
    }

    public static int PriceOf(Card card) => card.Sell * PriceMultiplier;

    /// <summary>Compra do estoque do dia. Null se a carta não está à venda ou falta ouro.</summary>
    public static Profile? Buy(Profile profile, string cardId, DateTimeOffset day, CardCatalog cards)
    {
        if (!StockFor(day, cards).Contains(cardId))
            return null;

        var price = PriceOf(cards.Get(cardId));
        if (profile.Gold < price)
            return null;

        return profile with { Gold = profile.Gold - price, Inventory = profile.Inventory.Append(cardId).ToList() };
    }

    /// <summary>Vende uma carta do inventário que não está no loadout. Null se não pode.</summary>
    public static Profile? Sell(Profile profile, string cardId, CardCatalog cards)
    {
        var inUse = profile.Loadout.Hand.Concat(profile.Loadout.EquippedIds()).Count(id => id == cardId);
        var owned = profile.Inventory.Count(id => id == cardId);
        if (owned <= inUse || !cards.Contains(cardId))
            return null;

        var inventory = profile.Inventory.ToList();
        inventory.Remove(cardId);
        return profile with { Gold = profile.Gold + cards.Get(cardId).Sell, Inventory = inventory };
    }

    /// <summary>Deposita ouro na guilda (protegido da perda ao cair). Null se não há tanto ouro.</summary>
    public static Profile? Deposit(Profile profile, int amount)
    {
        if (amount <= 0 || amount > profile.Gold)
            return null;

        return profile with { Gold = profile.Gold - amount, BankedGold = profile.BankedGold + amount };
    }

    private static List<string> Pool(CardCatalog cards, Rarity rarity) =>
        cards.Where(c => c.Rarity == rarity && c.Type != CardType.Treasure).Select(c => c.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();
}
