using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Rules;

namespace BattleHunter.Core.Cards;

/// <summary>Sorteia uma carta: primeiro a raridade (pesada pela SOR), depois uma carta daquela raridade.</summary>
public static class LootRoller
{
    public static Card Roll(LootTable table, CardCatalog catalog, int luck, IRandom random)
    {
        var rarity = RollRarity(table, luck, random);
        var pool = Pool(table, catalog, rarity);

        // Sem carta daquela raridade na tabela: cai para a raridade abaixo até achar.
        while (pool.Count == 0 && rarity > Rarity.Common)
        {
            rarity--;
            pool = Pool(table, catalog, rarity);
        }

        if (pool.Count == 0)
            throw new InvalidOperationException($"Tabela de loot '{table.Id}' não tem cartas elegíveis.");

        return pool[random.Next(pool.Count)];
    }

    public static Rarity RollRarity(LootTable table, int luck, IRandom random)
    {
        var weights = table.Rarities.Select(r => (r.Rarity, Weight: Math.Max(0, r.EffectiveWeight(luck)))).ToList();
        var total = weights.Sum(w => w.Weight);
        if (total <= 0)
            throw new InvalidOperationException($"Tabela de loot '{table.Id}' sem pesos positivos.");

        var roll = random.Next(total);
        foreach (var (rarity, weight) in weights)
        {
            if (roll < weight)
                return rarity;
            roll -= weight;
        }

        return weights[weights.Count - 1].Rarity;
    }

    private static IReadOnlyList<Card> Pool(LootTable table, CardCatalog catalog, Rarity rarity) =>
        catalog.Where(c => c.Rarity == rarity && table.Types.Contains(c.Type))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();
}
