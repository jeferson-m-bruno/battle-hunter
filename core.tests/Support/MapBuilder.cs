using BattleHunter.Core.Map;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Tests.Support;

/// <summary>
/// Monta um GridMap a partir de linhas ASCII: '.' chão, '#' parede, 'E' saída, 'C' baú.
/// A primeira linha é y = 0.
/// </summary>
public static class MapBuilder
{
    public static GridMap FromAscii(params string[] rows)
    {
        var height = rows.Length;
        var width = rows[0].Length;
        var cells = new Cell[width * height];

        for (var y = 0; y < height; y++)
        {
            if (rows[y].Length != width)
                throw new ArgumentException($"Linha {y} tem largura {rows[y].Length}, esperado {width}.");

            for (var x = 0; x < width; x++)
            {
                cells[y * width + x] = rows[y][x] switch
                {
                    '.' => Cell.Floor,
                    '#' => Cell.Wall,
                    'E' => Cell.Exit,
                    'C' => Cell.Chest,
                    var c => throw new ArgumentException($"Caractere desconhecido '{c}' em ({x},{y})."),
                };
            }
        }

        return new GridMap(width, height, cells, Array.Empty<Room>());
    }

    /// <summary>Mapa 5×5 só de chão, com saída em (4,4).</summary>
    public static GridMap Open5x5() => FromAscii(
        ".....",
        ".....",
        ".....",
        ".....",
        "....E");

    /// <summary>Mapa 5×5 com baús em (1,0) e (3,0) e saída em (4,4).</summary>
    public static GridMap WithChests5x5() => FromAscii(
        ".C.C.",
        ".....",
        ".....",
        ".....",
        "....E");
}
