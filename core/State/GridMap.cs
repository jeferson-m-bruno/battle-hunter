using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Map;

namespace BattleHunter.Core.State;

/// <summary>
/// Grid retangular de células, indexado por linha (y * Width + x), mais as salas que o gerador criou.
/// Imutável: a geração por seed cria um novo mapa; as regras só leem.
/// </summary>
public sealed record GridMap(int Width, int Height, IReadOnlyList<Cell> Cells, IReadOnlyList<Room> Rooms)
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

    /// <summary>Célula onde um caçador pode pisar: chão, saída ou spawn de monstro. Paredes e baús bloqueiam.</summary>
    public bool IsWalkable(Position position) =>
        IsInside(position) && this[position] is Cell.Floor or Cell.Exit or Cell.MonsterSpawn;

    public Room? RoomAt(Position position) => Rooms.FirstOrDefault(r => r.Contains(position));
}
