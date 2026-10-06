using System;
using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>BFS no grid com custo 1 por célula. A* (fatia 4) entra ao lado.</summary>
public static class Pathfinding
{
    private static readonly (int Dx, int Dy)[] Directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    public static IEnumerable<Position> Neighbors(Position p)
    {
        foreach (var (dx, dy) in Directions)
            yield return new Position(p.X + dx, p.Y + dy);
    }

    /// <summary>Distância em passos de <paramref name="from"/> a cada célula alcançável.</summary>
    public static IReadOnlyDictionary<Position, int> Distances(GridMap map, Position from, Func<Position, bool>? isPassable = null)
    {
        var passable = isPassable ?? map.IsWalkable;
        var dist = new Dictionary<Position, int> { [from] = 0 };
        var queue = new Queue<Position>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in Neighbors(current))
            {
                if (dist.ContainsKey(next) || !map.IsInside(next) || !passable(next))
                    continue;

                dist[next] = dist[current] + 1;
                queue.Enqueue(next);
            }
        }

        return dist;
    }

    /// <summary>
    /// Passos até uma célula alvo. Se o alvo não é pisável (baú), conta até o vizinho mais perto + 1.
    /// Null se inalcançável.
    /// </summary>
    public static int? StepsTo(IReadOnlyDictionary<Position, int> distances, Position target)
    {
        if (distances.TryGetValue(target, out var d))
            return d;

        int? best = null;
        foreach (var n in Neighbors(target))
        {
            if (distances.TryGetValue(n, out var nd) && (best == null || nd + 1 < best))
                best = nd + 1;
        }

        return best;
    }
}
