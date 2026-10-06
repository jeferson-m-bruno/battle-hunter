using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>O que cada caçador traz da guilda para a partida (GDD: até 5 cartas na mão).</summary>
public sealed record HunterSetup(
    int Id,
    string Name,
    HunterStats Stats,
    IReadOnlyList<string>? Hand = null,
    Equipment? Equipment = null,
    int Level = 1)
{
    public const int MaxStartingHand = 5;
}

/// <summary>
/// Monta o GameState inicial a partir do mapa gerado: caçadores nos spawns, baús (com mímicos sorteados),
/// tesouro-alvo e os monstros iniciais, um por sala sem caçador.
/// </summary>
public static class GameSetup
{
    public static GameState Create(
        GameConfig config,
        GeneratedMap map,
        IReadOnlyList<HunterSetup> hunters,
        string targetTreasureCardId,
        IRandom random,
        GameContent content)
    {
        if (hunters.Count < 1 || hunters.Count > map.HunterSpawns.Count)
            throw new ArgumentException($"A partida aceita de 1 a {map.HunterSpawns.Count} caçadores.", nameof(hunters));

        if (hunters.Any(h => (h.Hand?.Count ?? 0) > HunterSetup.MaxStartingHand))
            throw new ArgumentException($"Cada caçador leva no máximo {HunterSetup.MaxStartingHand} cartas.", nameof(hunters));

        var placed = hunters
            .Select((h, i) => Hunter.Create(h.Id, h.Name, h.Stats, map.HunterSpawns[i], h.Hand, h.Equipment, h.Level))
            .ToList();

        var chests = map.Chests
            .Select(p =>
            {
                var isTarget = p == map.TargetChest;
                var isMimic = !isTarget && random.Next(100) < config.MimicChancePercent;
                return new Chest(p, IsOpened: false, HoldsTargetTreasure: isTarget, IsMimic: isMimic);
            })
            .ToList();

        var state = GameState.New(config, map.Map, placed, chests, targetTreasureCardId, monsterSpawns: map.MonsterSpawns);
        return MonsterRules.SpawnInitial(state, random, content, new List<GameEvent>());
    }
}
