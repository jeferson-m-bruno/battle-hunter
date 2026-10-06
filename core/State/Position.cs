using System;

namespace BattleHunter.Core.State;

public sealed record Position(int X, int Y)
{
    /// <summary>Distância de Manhattan.</summary>
    public int DistanceTo(Position other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>Vizinho ortogonal (cima, baixo, esquerda ou direita), nunca diagonal.</summary>
    public bool IsOrthogonallyAdjacentTo(Position other) => DistanceTo(other) == 1;
}
