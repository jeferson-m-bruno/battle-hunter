using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>Resultado da geração: o grid e os pontos de interesse que o setup da partida usa.</summary>
public sealed record GeneratedMap(
    GridMap Map,
    IReadOnlyList<Position> HunterSpawns,
    Position Exit,
    IReadOnlyList<Position> Chests,
    Position TargetChest,
    IReadOnlyList<Position> MonsterSpawns);
