using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Map;

public class VisibilityTests
{
    [Fact]
    public void Given_NoRoom_When_Computed_Then_SeesManhattanRadiusTwo()
    {
        var map = MapBuilder.Open5x5();

        var visible = Visibility.VisibleCells(map, new Position(2, 2));

        Assert.Equal(13, visible.Count);
        Assert.Contains(new Position(2, 0), visible);
        Assert.Contains(new Position(3, 3), visible);
        Assert.DoesNotContain(new Position(4, 4), visible);
    }

    [Fact]
    public void Given_InsideRoom_When_Computed_Then_SeesWholeRoomPlusRadius()
    {
        var cells = Enumerable.Repeat(Cell.Floor, 12 * 12).ToList();
        var room = new Room(1, 1, 6, 4);
        var map = new GridMap(12, 12, cells, new[] { room });

        var visible = Visibility.VisibleCells(map, new Position(1, 1));

        Assert.All(room.Cells(), c => Assert.Contains(c, visible));
        Assert.Contains(new Position(1, 3), visible);
        Assert.DoesNotContain(new Position(8, 8), visible);
    }

    [Fact]
    public void Given_EdgeOfGrid_When_Computed_Then_OnlyCellsInsideGrid()
    {
        var map = MapBuilder.Open5x5();

        var visible = Visibility.VisibleCells(map, new Position(0, 0));

        Assert.All(visible, p => Assert.True(map.IsInside(p)));
        Assert.Equal(6, visible.Count);
    }
}
