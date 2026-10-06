using System;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>Linha de visão por Bresenham: nenhuma parede entre as duas células (baús e criaturas não bloqueiam).</summary>
public static class LineOfSight
{
    public static bool IsClear(GridMap map, Position from, Position to)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var x1 = to.X;
        var y1 = to.Y;
        var dx = Math.Abs(x1 - x0);
        var dy = -Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            var p = new Position(x0, y0);
            if (p != from && p != to && map[p] == Cell.Wall)
                return false;

            if (x0 == x1 && y0 == y1)
                return true;

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
