using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards.Effects;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>
/// Conteúdo estático de data/ que as regras consultam: cartas, monstros, tabelas de loot, efeitos, pesos da IA,
/// missões e níveis. Não faz parte do GameState (que guarda só ids); é passado ao Reducer.
/// </summary>
public sealed class GameContent
{
    public const string ChestLootTableId = "chest";

    public GameContent(
        CardCatalog cards,
        MonsterCatalog monsters,
        IEnumerable<LootTable> lootTables,
        EffectRegistry effects,
        AiWeights? ai = null,
        MissionCatalog? missions = null,
        LevelTable? levels = null)
    {
        Cards = cards;
        Monsters = monsters;
        LootTables = lootTables.ToDictionary(t => t.Id);
        Effects = effects;
        Ai = ai ?? AiWeights.Default;
        Missions = missions ?? MissionCatalog.Default;
        Levels = levels ?? LevelTable.Default;
    }

    public CardCatalog Cards { get; }
    public MonsterCatalog Monsters { get; }
    public IReadOnlyDictionary<string, LootTable> LootTables { get; }
    public EffectRegistry Effects { get; }
    public AiWeights Ai { get; }
    public MissionCatalog Missions { get; }
    public LevelTable Levels { get; }

    public LootTable LootTable(string id) =>
        LootTables.TryGetValue(id, out var table)
            ? table
            : throw new KeyNotFoundException($"Tabela de loot desconhecida: '{id}'.");

    public static GameContent FromJson(
        string cardsJson,
        string monstersJson,
        string lootTablesJson,
        EffectRegistry? effects = null,
        string? aiWeightsJson = null,
        string? missionsJson = null,
        string? levelsJson = null)
    {
        var cards = CardCatalog.FromJson(cardsJson);
        var monsters = MonsterCatalog.FromJson(monstersJson);
        var tables = JsonConvert.DeserializeObject<List<LootTable>>(lootTablesJson, JsonSettings.Default)
                     ?? new List<LootTable>();
        var ai = aiWeightsJson != null ? AiWeights.FromJson(aiWeightsJson) : null;
        var missions = missionsJson != null ? MissionCatalog.FromJson(missionsJson) : null;
        var levels = levelsJson != null ? LevelTable.FromJson(levelsJson) : null;
        return new GameContent(cards, monsters, tables, effects ?? EffectRegistry.Default, ai, missions, levels);
    }

    /// <summary>Carrega todos os JSON de uma pasta data/ (ai_weights, missions e levels são opcionais).</summary>
    public static GameContent LoadFromDirectory(string dataDirectory, EffectRegistry? effects = null)
    {
        string? Optional(string name)
        {
            var path = Path.Combine(dataDirectory, name);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        var missions = Optional("missions.json");
        var levels = Optional("levels.json");
        return FromJson(
            File.ReadAllText(Path.Combine(dataDirectory, "cards.json")),
            File.ReadAllText(Path.Combine(dataDirectory, "monsters.json")),
            File.ReadAllText(Path.Combine(dataDirectory, "loot_tables.json")),
            effects,
            Optional("ai_weights.json"),
            string.IsNullOrWhiteSpace(missions) || missions.Trim() == "[]" ? null : missions,
            string.IsNullOrWhiteSpace(levels) || levels.Trim() == "[]" ? null : levels);
    }
}
