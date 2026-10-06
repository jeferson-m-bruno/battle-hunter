using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class MovementRulesTests
{
    [Fact]
    public void Given_Acting_When_MovesToAdjacentFloor_Then_PositionChangesAndCostsOnePa()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Equal(new Position(1, 0), result.State.Hunter(1).Position);
        Assert.Equal(3, result.State.ActionPoints);
        var moved = Assert.Single(result.Events.OfType<HunterMoved>());
        Assert.Equal((new Position(0, 0), new Position(1, 0), 3), (moved.From, moved.To, moved.ActionPointsLeft));
    }

    [Fact]
    public void Given_Acting_When_MovesDiagonally_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new MoveTo(1, new Position(1, 1)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(new Position(0, 0), result.State.Hunter(1).Position);
    }

    [Fact]
    public void Given_Acting_When_MovesTwoCells_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new MoveTo(1, new Position(2, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_WallAhead_When_MovesIntoWall_Then_Rejected()
    {
        var map = MapBuilder.FromAscii(
            ".#.",
            "...",
            "..E");
        var state = NewGame(map, HunterAt(1, 0, 0), HunterAt(2, 2, 0));
        state = state.Apply(new StartGame(), new FixedRandom()).State;
        state = state.Apply(new RollDice(1), new FixedRandom(4)).State;

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(4, result.State.ActionPoints);
    }

    [Fact]
    public void Given_EdgeOfGrid_When_MovesOutside_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new MoveTo(1, new Position(-1, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_OtherHunterAdjacent_When_MovesOntoThem_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 1, 0));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_NoActionPoints_When_Moves_Then_Rejected()
    {
        // Dado 1 → 1 PA; primeiro movimento gasta tudo e o turno passa; o segundo vem fora do turno.
        var state = Rolled(new FixedRandom(1), HunterAt(1, 0, 0), HunterAt(2, 4, 0));
        state = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom()).State;

        var result = state.Apply(new MoveTo(1, new Position(2, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(new Position(1, 0), result.State.Hunter(1).Position);
    }

    [Fact]
    public void Given_LastActionPoint_When_Moves_Then_TurnEndsAutomatically()
    {
        var state = Rolled(new FixedRandom(1), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Equal(2, result.State.CurrentHunterId);
        Assert.Equal(GamePhase.AwaitingRoll, result.State.Phase);
        Assert.Contains(result.Events, e => e is TurnEnded t && t.HunterId == 1);
    }

    [Fact]
    public void Given_NotCurrentHunter_When_Moves_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new MoveTo(2, new Position(3, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(new Position(4, 0), result.State.Hunter(2).Position);
    }
}
