using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards.Effects;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>
/// Conteúdo estático de data/ que as regras consultam: cartas, monstros, tabelas de loot, efeitos e pesos da IA.
/// Não faz parte do GameState (que guarda só ids); é passado ao Reducer.
/// </summary>
public sealed class GameContent
{
    public const string ChestLootTableId = "chest";

    public GameContent(CardCatalog cards, MonsterCatalog monsters, IEnumerable<LootTable> lootTables, EffectRegistry effects, AiWeights? ai = null)
    {
        Cards = cards;
        Monsters = monsters;
        LootTables = lootTables.ToDictionary(t => t.Id);
        Effects = effects;
        Ai = ai ?? AiWeights.Default;
    }

    public CardCatalog Cards { get; }
    public MonsterCatalog Monsters { get; }
    public IReadOnlyDictionary<string, LootTable> LootTables { get; }
    public EffectRegistry Effects { get; }
    public AiWeights Ai { get; }

    public LootTable LootTable(string id) =>
        LootTables.TryGetValue(id, out var table)
            ? table
            : throw new KeyNotFoundException($"Tabela de loot desconhecida: '{id}'.");

    public static GameContent FromJson(string cardsJson, string monstersJson, string lootTablesJson, EffectRegistry? effects = null, string? aiWeightsJson = null)
    {
        var cards = CardCatalog.FromJson(cardsJson);
        var monsters = MonsterCatalog.FromJson(monstersJson);
        var tables = JsonConvert.DeserializeObject<List<LootTable>>(lootTablesJson, JsonSettings.Default)
                     ?? new List<LootTable>();
        var ai = aiWeightsJson != null ? AiWeights.FromJson(aiWeightsJson) : null;
        return new GameContent(cards, monsters, tables, effects ?? EffectRegistry.Default, ai);
    }

    /// <summary>Carrega cards.json, monsters.json, loot_tables.json e ai_weights.json de uma pasta data/.</summary>
    public static GameContent LoadFromDirectory(string dataDirectory, EffectRegistry? effects = null)
    {
        var aiPath = Path.Combine(dataDirectory, "ai_weights.json");
        return FromJson(
            File.ReadAllText(Path.Combine(dataDirectory, "cards.json")),
            File.ReadAllText(Path.Combine(dataDirectory, "monsters.json")),
            File.ReadAllText(Path.Combine(dataDirectory, "loot_tables.json")),
            effects,
            File.Exists(aiPath) ? File.ReadAllText(aiPath) : null);
    }
}
