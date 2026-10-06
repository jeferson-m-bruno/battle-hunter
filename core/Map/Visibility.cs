using System;
using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>Visão (GDD): o caçador vê a sala em que está e as células a até 2 passos; o resto é névoa.</summary>
public static class Visibility
{
    public const int Radius = 2;

    public static IReadOnlyCollection<Position> VisibleCells(GridMap map, Position from)
    {
        var visible = new HashSet<Position>();

        var room = map.RoomAt(from);
        if (room != null)
            visible.UnionWith(room.Cells());

        for (var dx = -Radius; dx <= Radius; dx++)
        {
            for (var dy = -Radius; dy <= Radius; dy++)
            {
                if (Math.Abs(dx) + Math.Abs(dy) > Radius)
                    continue;

                var p = new Position(from.X + dx, from.Y + dy);
                if (map.IsInside(p))
                    visible.Add(p);
            }
        }

        return visible;
    }
}
