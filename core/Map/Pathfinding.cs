using System;
using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>BFS (distâncias) e A* (caminho) no grid, custo 1 por célula, 4 vizinhos.</summary>
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

    /// <summary>
    /// A* com heurística de Manhattan. Devolve o caminho de <paramref name="from"/> (exclusive) até
    /// <paramref name="to"/> (inclusive), ou null se não há caminho. O destino sempre conta como passável;
    /// desempate determinístico (menor f, depois menor h, depois y, depois x).
    /// </summary>
    public static IReadOnlyList<Position>? AStar(GridMap map, Position from, Position to, Func<Position, bool>? isPassable = null)
    {
        var passable = isPassable ?? map.IsWalkable;
        if (!map.IsInside(to))
            return null;

        if (from == to)
            return Array.Empty<Position>();

        var open = new SortedSet<(int F, int H, int Y, int X)>();
        var g = new Dictionary<Position, int> { [from] = 0 };
        var cameFrom = new Dictionary<Position, Position>();
        var closed = new HashSet<Position>();

        open.Add((from.DistanceTo(to), from.DistanceTo(to), from.Y, from.X));

        while (open.Count > 0)
        {
            var best = open.Min;
            open.Remove(best);
            var current = new Position(best.X, best.Y);

            if (current == to)
                return Reconstruct(cameFrom, to);

            if (!closed.Add(current))
                continue;

            foreach (var next in Neighbors(current))
            {
                if (closed.Contains(next) || !map.IsInside(next))
                    continue;

                if (next != to && !passable(next))
                    continue;

                var tentative = g[current] + 1;
                if (g.TryGetValue(next, out var known) && tentative >= known)
                    continue;

                g[next] = tentative;
                cameFrom[next] = current;
                var h = next.DistanceTo(to);
                open.Add((tentative + h, h, next.Y, next.X));
            }
        }

        return null;
    }

    private static List<Position> Reconstruct(Dictionary<Position, Position> cameFrom, Position to)
    {
        var path = new List<Position> { to };
        var current = to;
        while (cameFrom.TryGetValue(current, out var prev))
        {
            if (!cameFrom.ContainsKey(prev))
                break;
            path.Add(prev);
            current = prev;
        }

        path.Reverse();
        return path;
    }
}
