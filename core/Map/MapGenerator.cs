using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Map;

/// <summary>
/// Gera o dungeon a partir de um IRandom com seed (GDD, seção Mapa):
/// uma sala por quadrante (spawn de caçador no canto) mais salas extras, corredores em L
/// entre salas consecutivas, saída fora das salas de spawn, baús em salas,
/// tesouro-alvo no baú mais distante da média dos spawns (±2), spawn de monstro por sala sem caçador.
/// Resultado validado por BFS; tentativa inválida é descartada e o gerador tenta de novo.
/// </summary>
public static class MapGenerator
{
    private const int MaxAttempts = 200;
    private const int MinRoomSide = 3;
    private const int MaxRoomSide = 6;
    private const int TargetChestTolerance = 2;

    public static GeneratedMap Generate(MapSpec spec, IRandom random)
    {
        if (spec.Size < 12 || spec.MinRooms < MapSpec.HunterSpawns + 1)
            throw new ArgumentException("Grid mínimo 12×12 e ao menos 5 salas (4 de spawn + 1 para a saída).", nameof(spec));

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var map = TryGenerate(spec, random);
            if (map != null)
                return map;
        }

        throw new InvalidOperationException($"Não foi possível gerar um mapa válido em {MaxAttempts} tentativas.");
    }

    private static GeneratedMap? TryGenerate(MapSpec spec, IRandom random)
    {
        var size = spec.Size;
        var rooms = PlaceRooms(spec, random);
        if (rooms == null)
            return null;

        var cells = new Cell[size * size];
        Array.Fill(cells, Cell.Wall);

        foreach (var room in rooms)
            foreach (var p in room.Cells())
                cells[Index(p, size)] = Cell.Floor;

        for (var i = 1; i < rooms.Count; i++)
            CarveCorridor(cells, size, rooms[i - 1].Center, rooms[i].Center, random);

        var spawns = CornerSpawns(rooms, size);

        var exitRoom = rooms[MapSpec.HunterSpawns + random.Next(rooms.Count - MapSpec.HunterSpawns)];
        var exit = RandomCell(exitRoom, random);
        cells[Index(exit, size)] = Cell.Exit;

        var chestCount = spec.MinChests + random.Next(spec.MaxChests - spec.MinChests + 1);
        var reserved = new HashSet<Position>(spawns) { exit };
        var candidates = random.Shuffle(rooms.SelectMany(r => r.Cells()).Where(p => !reserved.Contains(p)).ToList());
        if (candidates.Count < chestCount)
            return null;

        var chests = candidates.Take(chestCount).ToList();
        foreach (var chest in chests)
        {
            cells[Index(chest, size)] = Cell.Chest;
            reserved.Add(chest);
        }

        var monsterSpawns = new List<Position>();
        foreach (var room in rooms.Skip(MapSpec.HunterSpawns))
        {
            var free = room.Cells().Where(p => !reserved.Contains(p)).ToList();
            if (free.Count == 0)
                continue;

            var spawn = free[random.Next(free.Count)];
            cells[Index(spawn, size)] = Cell.MonsterSpawn;
            monsterSpawns.Add(spawn);
            reserved.Add(spawn);
        }

        var grid = new GridMap(size, size, cells, rooms);

        if (!IsConnected(grid, spawns, exit, chests))
            return null;

        var target = PickTargetChest(grid, rooms, spawns, chests, random);
        if (target == null)
            return null;

        return new GeneratedMap(grid, spawns, exit, chests, target, monsterSpawns);
    }

    /// <summary>
    /// Uma sala ancorada em cada canto do grid (índices 0 a 3, na ordem dos cantos) e depois as extras
    /// em qualquer lugar. Sem borda de parede: a beira do grid já delimita o dungeon.
    /// </summary>
    private static List<Room>? PlaceRooms(MapSpec spec, IRandom random)
    {
        var size = spec.Size;
        var half = size / 2;
        var rooms = new List<Room>();

        // Lado máximo das salas de canto: 3 no 12×12, 4 no 14×14, 5 no 16×16. Sobra faixa central para as extras.
        var cornerMax = Math.Max(MinRoomSide, half - 3);
        var extraMax = Math.Min(MaxRoomSide, size / 3);

        foreach (var (left, top) in new[] { (true, true), (false, true), (true, false), (false, false) })
        {
            var w = MinRoomSide + random.Next(cornerMax - MinRoomSide + 1);
            var h = MinRoomSide + random.Next(cornerMax - MinRoomSide + 1);
            rooms.Add(new Room(left ? 0 : size - w, top ? 0 : size - h, w, h));
        }

        var total = spec.MinRooms + random.Next(spec.MaxRooms - spec.MinRooms + 1);
        for (var i = rooms.Count; i < total; i++)
        {
            var room = TryPlaceRoom(rooms, 0, 0, size, size, extraMax, random, tries: 40);
            if (room != null)
                rooms.Add(room);
        }

        return rooms.Count >= spec.MinRooms ? rooms : null;
    }

    private static Room? TryPlaceRoom(List<Room> existing, int minX, int minY, int maxX, int maxY, int maxSide, IRandom random, int tries)
    {
        var areaW = maxX - minX;
        var areaH = maxY - minY;
        if (areaW < MinRoomSide || areaH < MinRoomSide)
            return null;

        for (var t = 0; t < tries; t++)
        {
            var w = MinRoomSide + random.Next(Math.Min(maxSide, areaW) - MinRoomSide + 1);
            var h = MinRoomSide + random.Next(Math.Min(maxSide, areaH) - MinRoomSide + 1);
            var x = minX + random.Next(areaW - w + 1);
            var y = minY + random.Next(areaH - h + 1);
            var room = new Room(x, y, w, h);

            if (!existing.Any(r => r.Intersects(room, margin: 1)))
                return room;
        }

        return null;
    }

    /// <summary>Corredor em L: horizontal e depois vertical, ou o inverso, sorteado.</summary>
    private static void CarveCorridor(Cell[] cells, int size, Position from, Position to, IRandom random)
    {
        var corner = random.Next(2) == 0 ? new Position(to.X, from.Y) : new Position(from.X, to.Y);
        CarveLine(cells, size, from, corner);
        CarveLine(cells, size, corner, to);
    }

    private static void CarveLine(Cell[] cells, int size, Position a, Position b)
    {
        var dx = Math.Sign(b.X - a.X);
        var dy = Math.Sign(b.Y - a.Y);
        var p = a;
        while (true)
        {
            if (cells[Index(p, size)] == Cell.Wall)
                cells[Index(p, size)] = Cell.Floor;

            if (p == b)
                break;

            p = new Position(p.X + dx, p.Y + dy);
        }
    }

    /// <summary>Spawn de cada caçador: a célula da sala do quadrante mais próxima do canto.</summary>
    private static List<Position> CornerSpawns(List<Room> rooms, int size)
    {
        var corners = new[] { new Position(0, 0), new Position(size - 1, 0), new Position(0, size - 1), new Position(size - 1, size - 1) };
        return corners.Select((c, i) =>
        {
            var r = rooms[i];
            return new Position(Math.Clamp(c.X, r.X, r.Right), Math.Clamp(c.Y, r.Y, r.Bottom));
        }).ToList();
    }

    private static Position RandomCell(Room room, IRandom random)
    {
        var cells = room.Cells().ToList();
        return cells[random.Next(cells.Count)];
    }

    /// <summary>Todas as células pisáveis são alcançáveis a partir do primeiro spawn, e todo baú tem um vizinho alcançável.</summary>
    private static bool IsConnected(GridMap map, IReadOnlyList<Position> spawns, Position exit, IReadOnlyList<Position> chests)
    {
        var dist = Pathfinding.Distances(map, spawns[0]);

        for (var y = 0; y < map.Height; y++)
            for (var x = 0; x < map.Width; x++)
            {
                var p = new Position(x, y);
                if (map.IsWalkable(p) && !dist.ContainsKey(p))
                    return false;
            }

        return spawns.All(dist.ContainsKey)
               && dist.ContainsKey(exit)
               && chests.All(c => Pathfinding.StepsTo(dist, c) != null);
    }

    /// <summary>Baú fora das salas de spawn cuja distância média aos spawns fica a ≤ 2 passos da maior.</summary>
    private static Position? PickTargetChest(GridMap map, List<Room> rooms, IReadOnlyList<Position> spawns, IReadOnlyList<Position> chests, IRandom random)
    {
        var spawnRooms = rooms.Take(MapSpec.HunterSpawns).ToList();
        var distances = spawns.Select(s => Pathfinding.Distances(map, s)).ToList();

        var scored = chests
            .Where(c => !spawnRooms.Any(r => r.Contains(c)))
            .Select(c => (Chest: c, Mean: distances.Average(d => Pathfinding.StepsTo(d, c) ?? 0)))
            .ToList();

        if (scored.Count == 0)
            return null;

        var best = scored.Max(s => s.Mean);
        var candidates = scored.Where(s => s.Mean >= best - TargetChestTolerance).Select(s => s.Chest).ToList();
        return candidates[random.Next(candidates.Count)];
    }

    private static int Index(Position p, int size) => p.Y * size + p.X;
}
