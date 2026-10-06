using System.Collections.Generic;

namespace BattleHunter.Core.Cards;

/// <summary>Uma tabela de data/loot_tables.json: tipos de carta elegíveis e peso por raridade.</summary>
public sealed record LootTable(string Id, IReadOnlyList<CardType> Types, IReadOnlyList<RarityWeight> Rarities);

/// <summary>Peso efetivo = Weight + PerLuck × SOR de quem abre.</summary>
public sealed record RarityWeight(Rarity Rarity, int Weight, int PerLuck)
{
    public int EffectiveWeight(int luck) => Weight + PerLuck * luck;
}
