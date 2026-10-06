using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Rules;

/// <summary>O que cada caçador traz da guilda para a partida (GDD: até 5 cartas na mão).</summary>
public sealed record HunterSetup(
    int Id,
    string Name,
    HunterStats Stats,
    IReadOnlyList<string>? Hand = null,
    Equipment? Equipment = null)
{
    public const int MaxStartingHand = 5;
}

/// <summary>Monta o GameState inicial a partir do mapa gerado: caçadores nos spawns, baús e tesouro-alvo.</summary>
public static class GameSetup
{
    public static GameState Create(GameConfig config, GeneratedMap map, IReadOnlyList<HunterSetup> hunters, string targetTreasureCardId)
    {
        if (hunters.Count < 1 || hunters.Count > map.HunterSpawns.Count)
            throw new ArgumentException($"A partida aceita de 1 a {map.HunterSpawns.Count} caçadores.", nameof(hunters));

        if (hunters.Any(h => (h.Hand?.Count ?? 0) > HunterSetup.MaxStartingHand))
            throw new ArgumentException($"Cada caçador leva no máximo {HunterSetup.MaxStartingHand} cartas.", nameof(hunters));

        var placed = hunters
            .Select((h, i) => Hunter.Create(h.Id, h.Name, h.Stats, map.HunterSpawns[i], h.Hand, h.Equipment))
            .ToList();

        var chests = map.Chests
            .Select(p => new Chest(p, IsOpened: false, HoldsTargetTreasure: p == map.TargetChest))
            .ToList();

        return GameState.New(config, map.Map, placed, chests, targetTreasureCardId);
    }
}
