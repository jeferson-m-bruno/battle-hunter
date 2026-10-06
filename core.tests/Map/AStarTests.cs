using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Map;

public class AStarTests
{
    private static readonly GridMap Map = MapBuilder.FromAscii(
        ".....",
        ".###.",
        ".....",
        ".#.#.",
        "....E");

    [Fact]
    public void Given_WallBetween_When_Searched_Then_FindsShortestDetour()
    {
        var path = Pathfinding.AStar(Map, new Position(0, 0), new Position(4, 0))!;

        Assert.Equal(4, path.Count);
        Assert.Equal(new Position(4, 0), path[^1]);
        Assert.All(path, p => Assert.True(Map.IsWalkable(p)));
    }

    [Fact]
    public void Given_LongerDetourNeeded_When_Searched_Then_PathLengthEqualsBfsDistance()
    {
        var from = new Position(0, 0);
        var to = new Position(2, 2);
        var dist = Pathfinding.Distances(Map, from);

        var path = Pathfinding.AStar(Map, from, to)!;

        Assert.Equal(dist[to], path.Count);
        for (var i = 1; i < path.Count; i++)
            Assert.True(path[i - 1].IsOrthogonallyAdjacentTo(path[i]));
    }

    [Fact]
    public void Given_Unreachable_When_Searched_Then_Null()
    {
        var sealedMap = MapBuilder.FromAscii(
            "..#..",
            "..#..",
            "..#..",
            "..#..",
            "..#.E");

        Assert.Null(Pathfinding.AStar(sealedMap, new Position(0, 0), new Position(4, 0)));
    }

    [Fact]
    public void Given_DestinationNotWalkable_When_Searched_Then_PathEndsOnIt()
    {
        var chests = MapBuilder.WithChests5x5();

        var path = Pathfinding.AStar(chests, new Position(0, 0), new Position(1, 0))!;

        Assert.Equal(new[] { new Position(1, 0) }, path);
    }

    [Fact]
    public void Given_SameCell_When_Searched_Then_EmptyPath()
    {
        Assert.Empty(Pathfinding.AStar(Map, new Position(2, 2), new Position(2, 2))!);
    }

    [Fact]
    public void Given_CustomPassable_When_Searched_Then_RespectsRestriction()
    {
        var open = MapBuilder.Open5x5();

        var path = Pathfinding.AStar(open, new Position(0, 0), new Position(0, 4), p => p.Y != 2 || p.X == 4);

        Assert.NotNull(path);
        Assert.Contains(new Position(4, 2), path!);
    }
}
