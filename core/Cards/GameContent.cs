using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleHunter.Core.Cards.Effects;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>
/// Conteúdo estático de data/ que as regras consultam: cartas, tabelas de loot e efeitos.
/// Não faz parte do GameState (que guarda só ids); é passado ao Reducer.
/// </summary>
public sealed class GameContent
{
    public const string ChestLootTableId = "chest";

    public GameContent(CardCatalog cards, IEnumerable<LootTable> lootTables, EffectRegistry effects)
    {
        Cards = cards;
        LootTables = lootTables.ToDictionary(t => t.Id);
        Effects = effects;
    }

    public CardCatalog Cards { get; }
    public IReadOnlyDictionary<string, LootTable> LootTables { get; }
    public EffectRegistry Effects { get; }

    public LootTable LootTable(string id) =>
        LootTables.TryGetValue(id, out var table)
            ? table
            : throw new KeyNotFoundException($"Tabela de loot desconhecida: '{id}'.");

    public static GameContent FromJson(string cardsJson, string lootTablesJson, EffectRegistry? effects = null)
    {
        var cards = CardCatalog.FromJson(cardsJson);
        var tables = JsonConvert.DeserializeObject<List<LootTable>>(lootTablesJson, JsonSettings.Default)
                     ?? new List<LootTable>();
        return new GameContent(cards, tables, effects ?? EffectRegistry.Default);
    }

    /// <summary>Carrega cards.json e loot_tables.json de uma pasta data/.</summary>
    public static GameContent LoadFromDirectory(string dataDirectory, EffectRegistry? effects = null) =>
        FromJson(
            File.ReadAllText(Path.Combine(dataDirectory, "cards.json")),
            File.ReadAllText(Path.Combine(dataDirectory, "loot_tables.json")),
            effects);
}
