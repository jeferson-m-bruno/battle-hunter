using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>Tipos de missão, carregados de data/missions.json.</summary>
public sealed class MissionCatalog
{
    private readonly Dictionary<string, MissionType> _missions;

    public MissionCatalog(IEnumerable<MissionType> missions)
    {
        _missions = missions.ToDictionary(m => m.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<MissionType> All => _missions.Values.OrderBy(m => m.RequiredLevel).ThenBy(m => m.Id, StringComparer.Ordinal).ToList();

    public bool Contains(string id) => _missions.ContainsKey(id);

    public MissionType Get(string id) =>
        _missions.TryGetValue(id, out var m) ? m : throw new KeyNotFoundException($"Missão desconhecida: '{id}'.");

    public static MissionCatalog FromJson(string json)
    {
        var list = JsonConvert.DeserializeObject<List<MissionType>>(json, JsonSettings.Default) ?? throw new JsonException("missions.json vazio.");
        return new MissionCatalog(list);
    }

    /// <summary>As 4 missões do GDD, para quem não tem data/ à mão (testes antigos).</summary>
    public static MissionCatalog Default { get; } = new(new[]
    {
        new MissionType("easy", "Caça (fácil)", 12, 30, false, 100, 100, 50, false, false, 1),
        new MissionType("normal", "Caça (normal)", 14, 30, true, 100, 250, 120, false, false, 5),
        new MissionType("hard", "Caça (difícil)", 16, 25, true, 150, 500, 250, true, false, 12),
        new MissionType("ranked", "Ranqueada", 14, 30, true, 100, 200, 0, false, true, 8),
    });
}
