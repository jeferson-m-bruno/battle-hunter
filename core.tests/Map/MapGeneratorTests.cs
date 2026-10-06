using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Tests.Map;

public class MapGeneratorTests
{
    [Fact]
    public void Given_SameSeed_When_Generated_Then_MapsAreIdentical()
    {
        var a = MapGenerator.Generate(new MapSpec(), new SeededRandom(123));
        var b = MapGenerator.Generate(new MapSpec(), new SeededRandom(123));

        Assert.Equal(a.Map.Cells, b.Map.Cells);
        Assert.Equal(a.HunterSpawns, b.HunterSpawns);
        Assert.Equal(a.Exit, b.Exit);
        Assert.Equal(a.Chests, b.Chests);
        Assert.Equal(a.TargetChest, b.TargetChest);
    }

    [Fact]
    public void Given_DifferentSeeds_When_Generated_Then_MapsDiffer()
    {
        var a = MapGenerator.Generate(new MapSpec(), new SeededRandom(1));
        var b = MapGenerator.Generate(new MapSpec(), new SeededRandom(2));

        Assert.NotEqual(a.Map.Cells, b.Map.Cells);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(16)]
    public void Given_ThousandSeeds_When_Generated_Then_AllAreConnectedAndWithinSpec(int size)
    {
        var spec = new MapSpec(Size: size);

        for (var seed = 1; seed <= 1000; seed++)
        {
            var g = MapGenerator.Generate(spec, new SeededRandom(seed));
            var dist = Pathfinding.Distances(g.Map, g.HunterSpawns[0]);

            Assert.InRange(g.Map.Rooms.Count, spec.MinRooms, spec.MaxRooms);
            Assert.InRange(g.Chests.Count, spec.MinChests, spec.MaxChests);
            Assert.Equal(4, g.HunterSpawns.Count);
            Assert.All(g.HunterSpawns, s => Assert.True(dist.ContainsKey(s), $"seed {seed}: spawn {s} inalcançável"));
            Assert.True(dist.ContainsKey(g.Exit), $"seed {seed}: saída inalcançável");
            Assert.All(g.Chests, c => Assert.NotNull(Pathfinding.StepsTo(dist, c)));
            Assert.Equal(Cell.Exit, g.Map[g.Exit]);
            Assert.All(g.Chests, c => Assert.Equal(Cell.Chest, g.Map[c]));
            Assert.Contains(g.TargetChest, g.Chests);
        }
    }

    [Fact]
    public void Given_Generated_When_Inspected_Then_SpawnsAreInDistinctRoomsNearCorners()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var g = MapGenerator.Generate(new MapSpec(), new SeededRandom(seed));
            var rooms = g.HunterSpawns.Select(s => g.Map.RoomAt(s)).ToList();

            Assert.All(rooms, r => Assert.NotNull(r));
            Assert.Equal(4, rooms.Distinct().Count());

            var size = g.Map.Width;
            Assert.True(g.HunterSpawns[0].X < size / 2 && g.HunterSpawns[0].Y < size / 2);
            Assert.True(g.HunterSpawns[3].X >= size / 2 && g.HunterSpawns[3].Y >= size / 2);
        }
    }

    [Fact]
    public void Given_Generated_When_Inspected_Then_ExitIsNotInASpawnRoom()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var g = MapGenerator.Generate(new MapSpec(), new SeededRandom(seed));
            var spawnRooms = g.HunterSpawns.Select(s => g.Map.RoomAt(s)).ToList();

            Assert.DoesNotContain(g.Map.RoomAt(g.Exit), spawnRooms);
        }
    }

    [Fact]
    public void Given_Generated_When_Inspected_Then_TargetChestIsFarthestFromSpawnsWithinTolerance()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var g = MapGenerator.Generate(new MapSpec(), new SeededRandom(seed));
            var spawnRooms = g.HunterSpawns.Select(s => g.Map.RoomAt(s)).ToList();
            var distances = g.HunterSpawns.Select(s => Pathfinding.Distances(g.Map, s)).ToList();

            double Mean(Position c) => distances.Average(d => Pathfinding.StepsTo(d, c)!.Value);
            var best = g.Chests.Where(c => !spawnRooms.Contains(g.Map.RoomAt(c))).Max(Mean);

            Assert.DoesNotContain(g.Map.RoomAt(g.TargetChest), spawnRooms);
            Assert.True(Mean(g.TargetChest) >= best - 2, $"seed {seed}: alvo a {Mean(g.TargetChest)} passos, máximo {best}");
        }
    }

    [Fact]
    public void Given_Generated_When_Inspected_Then_EveryNonSpawnRoomHasAMonsterSpawn()
    {
        var g = MapGenerator.Generate(new MapSpec(), new SeededRandom(77));
        var spawnRooms = g.HunterSpawns.Select(s => g.Map.RoomAt(s)).ToList();
        var otherRooms = g.Map.Rooms.Where(r => !spawnRooms.Contains(r)).ToList();

        Assert.Equal(otherRooms.Count, g.MonsterSpawns.Count);
        Assert.All(g.MonsterSpawns, m => Assert.Contains(g.Map.RoomAt(m), otherRooms));
    }

    [Fact]
    public void Given_SpecBelowMinimum_When_Generated_Then_Throws()
    {
        Assert.Throws<ArgumentException>(() => MapGenerator.Generate(new MapSpec(Size: 8), new SeededRandom(1)));
        Assert.Throws<ArgumentException>(() => MapGenerator.Generate(new MapSpec(MinRooms: 4), new SeededRandom(1)));
    }
}
