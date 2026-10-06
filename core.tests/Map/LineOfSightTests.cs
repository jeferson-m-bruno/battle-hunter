using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Map;

public class LineOfSightTests
{
    private static readonly GridMap Map = MapBuilder.FromAscii(
        ".....",
        "..#..",
        ".....",
        "..#..",
        "....E");

    [Fact]
    public void Given_OpenLine_When_Checked_Then_Clear()
    {
        Assert.True(LineOfSight.IsClear(Map, new Position(0, 0), new Position(4, 0)));
        Assert.True(LineOfSight.IsClear(Map, new Position(0, 2), new Position(4, 2)));
    }

    [Fact]
    public void Given_WallBetween_When_Checked_Then_Blocked()
    {
        Assert.False(LineOfSight.IsClear(Map, new Position(0, 1), new Position(4, 1)));
        Assert.False(LineOfSight.IsClear(Map, new Position(2, 0), new Position(2, 4)));
    }

    [Fact]
    public void Given_Diagonal_When_Checked_Then_WallOnPathBlocks()
    {
        Assert.False(LineOfSight.IsClear(Map, new Position(0, 3), new Position(4, 3)));
        Assert.True(LineOfSight.IsClear(Map, new Position(0, 0), new Position(4, 4)));
    }

    [Fact]
    public void Given_SameOrAdjacentCells_When_Checked_Then_Clear()
    {
        Assert.True(LineOfSight.IsClear(Map, new Position(1, 1), new Position(1, 1)));
        Assert.True(LineOfSight.IsClear(Map, new Position(1, 1), new Position(2, 1)));
    }
}
