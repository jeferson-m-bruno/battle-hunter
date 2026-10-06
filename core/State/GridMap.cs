using System;
using System.Collections.Generic;

namespace BattleHunter.Core.State;

/// <summary>
/// Grid retangular de células, indexado por linha (y * Width + x).
/// Imutável: a geração por seed (fatia 2) cria um novo mapa; as regras só leem.
/// </summary>
public sealed record GridMap(int Width, int Height, IReadOnlyList<Cell> Cells)
{
    public Cell this[Position position]
    {
        get
        {
            if (!IsInside(position))
                throw new ArgumentOutOfRangeException(nameof(position), $"Posição {position} fora do grid {Width}×{Height}.");

            return Cells[position.Y * Width + position.X];
        }
    }

    public bool IsInside(Position position) =>
        position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

    /// <summary>Célula onde um caçador pode pisar: tudo menos parede.</summary>
    public bool IsWalkable(Position position) => IsInside(position) && this[position] != Cell.Wall;
}
