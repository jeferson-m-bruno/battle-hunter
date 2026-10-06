using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>Todos os tipos de monstro, carregados de data/monsters.json.</summary>
public sealed class MonsterCatalog
{
    public const string MimicId = "mimic";
    public const string BossId = "dragon";

    private readonly Dictionary<string, MonsterType> _types;

    public MonsterCatalog(IEnumerable<MonsterType> types)
    {
        _types = types.ToDictionary(t => t.Id, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<MonsterType> All => _types.Values;

    public MonsterType Get(string id) =>
        _types.TryGetValue(id, out var type)
            ? type
            : throw new KeyNotFoundException($"Monstro desconhecido: '{id}'.");

    /// <summary>Tipos que nascem nos spawns (nem mímico nem chefe), em ordem de id para o sorteio ser determinístico.</summary>
    public IReadOnlyList<MonsterType> Spawnable => _types.Values.Where(t => t.Spawns).OrderBy(t => t.Id, StringComparer.Ordinal).ToList();

    public static MonsterCatalog FromJson(string json)
    {
        var types = JsonConvert.DeserializeObject<List<MonsterType>>(json, JsonSettings.Default)
                    ?? throw new JsonException("monsters.json vazio.");
        return new MonsterCatalog(types);
    }
}
