using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Ai;

/// <summary>Pesos e limiares da IA de caçador, de data/ai_weights.json. Balanceamento vive aqui, não em código.</summary>
public sealed class AiWeights
{
    public AiWeights(
        IReadOnlyDictionary<string, int> baseWeights,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> profiles,
        IReadOnlyDictionary<string, int> thresholds)
    {
        Base = baseWeights;
        Profiles = profiles;
        Thresholds = thresholds;
    }

    public IReadOnlyDictionary<string, int> Base { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Profiles { get; }
    public IReadOnlyDictionary<string, int> Thresholds { get; }

    /// <summary>Peso de uma situação para um perfil: base + bônus do perfil.</summary>
    public int Weight(AiProfile profile, string key)
    {
        var value = Base.TryGetValue(key, out var b) ? b : 0;
        if (Profiles.TryGetValue(ProfileKey(profile), out var bonus) && bonus.TryGetValue(key, out var extra))
            value += extra;
        return value;
    }

    public int Threshold(string key, int fallback) => Thresholds.TryGetValue(key, out var v) ? v : fallback;

    public static string ProfileKey(AiProfile profile) => profile.ToString().ToLowerInvariant();

    public static AiWeights FromJson(string json)
    {
        var dto = JsonConvert.DeserializeObject<Dto>(json, JsonSettings.Default) ?? throw new JsonException("ai_weights.json vazio.");
        return new AiWeights(
            dto.Base ?? new Dictionary<string, int>(),
            (dto.Profiles ?? new Dictionary<string, Dictionary<string, int>>()).ToDictionary(p => p.Key, p => (IReadOnlyDictionary<string, int>)p.Value, StringComparer.Ordinal),
            dto.Thresholds ?? new Dictionary<string, int>());
    }

    /// <summary>Pesos do GDD, usados quando data/ai_weights.json não está disponível.</summary>
    public static AiWeights Default { get; } = FromJson(@"{
        ""base"": { ""treasure_to_exit"": 100, ""hunt_marked"": 80, ""heal"": 70, ""cure"": 65, ""flee"": 60, ""open_chest"": 50, ""attack_monster"": 40, ""equip"": 35, ""avoid_boss"": 30, ""explore"": 10 },
        ""profiles"": { ""balanced"": {}, ""aggressive"": { ""hunt_marked"": 30, ""attack_monster"": 30 }, ""cautious"": { ""flee"": 30, ""heal"": 30, ""cure"": 30 }, ""greedy"": { ""open_chest"": 30 } },
        ""thresholds"": { ""hunt_marked_max_steps"": 3, ""hunt_marked_min_hp_percent"": 50, ""heal_below_hp_percent"": 30, ""flee_at_or_below_hp_percent"": 40, ""chest_max_steps"": 4, ""boss_min_hp_percent"": 60, ""avoid_boss_within_steps"": 3 }
    }");

    private sealed class Dto
    {
        public Dictionary<string, int>? Base { get; set; }
        public Dictionary<string, Dictionary<string, int>>? Profiles { get; set; }
        public Dictionary<string, int>? Thresholds { get; set; }
    }
}
