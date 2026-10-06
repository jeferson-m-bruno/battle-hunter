using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>Sala retangular; X,Y é o canto superior esquerdo, inclusive.</summary>
public sealed record Room(int X, int Y, int Width, int Height)
{
    public int Right => X + Width - 1;
    public int Bottom => Y + Height - 1;

    public Position Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Position p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;

    /// <summary>Verdadeiro se os retângulos se tocam ou sobrepõem, considerando uma margem ao redor.</summary>
    public bool Intersects(Room other, int margin = 0) =>
        X - margin <= other.Right && Right + margin >= other.X &&
        Y - margin <= other.Bottom && Bottom + margin >= other.Y;

    public IEnumerable<Position> Cells()
    {
        for (var y = Y; y <= Bottom; y++)
            for (var x = X; x <= Right; x++)
                yield return new Position(x, y);
    }
}
