using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class GroundRulesTests
{
    private const string Treasure = "treasure_dragon_eye";

    private static GameState WithGround(IRandom random, Position at, IReadOnlyList<string>? hand = null, params string[] cards)
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
            HunterAt(1, 0, 0, hand: hand), HunterAt(2, 4, 3));
        state = state with { GroundCards = cards.Select(c => new GroundCard(at, c)).ToList() };
        return state.StartAndRoll(random);
    }

    [Fact]
    public void Given_CardOnCurrentCell_When_PickedUp_Then_CostsOnePaAndGoesToHand()
    {
        var state = WithGround(new FixedRandom(4), new Position(0, 0), null, "dagger", "boots");

        var result = state.Apply(new PickUp(1, "boots"), new FixedRandom());

        Assert.Equal(3, result.State.ActionPoints);
        Assert.Equal(new[] { "boots" }, result.State.Hunter(1).Hand);
        Assert.Single(result.State.GroundCards, g => g.CardId == "dagger");
        Assert.Contains(result.Events, e => e is CardPickedUp p && p.CardId == "boots");
    }

    [Fact]
    public void Given_CardOnOtherCell_When_PickedUp_Then_Rejected()
    {
        var state = WithGround(new FixedRandom(4), new Position(1, 0), null, "dagger");

        var result = state.Apply(new PickUp(1, "dagger"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_FullHand_When_PickedUp_Then_Rejected()
    {
        var state = WithGround(new FixedRandom(4), new Position(0, 0), Enumerable.Repeat("dagger", 10).ToList(), "boots");

        var result = state.Apply(new PickUp(1, "boots"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Single(result.State.GroundCards);
    }

    [Fact]
    public void Given_TreasureOnGround_When_PickedUp_Then_HunterIsMarked()
    {
        var state = WithGround(new FixedRandom(4), new Position(0, 0), null, Treasure);

        var result = state.Apply(new PickUp(1, Treasure), new FixedRandom());

        Assert.True(result.State.IsMarked(1));
        Assert.Contains(result.Events, e => e is HunterMarked m && m.HunterId == 1);
    }

    [Fact]
    public void Given_LastPa_When_PickedUp_Then_TurnEnds()
    {
        var state = WithGround(new FixedRandom(1), new Position(0, 0), null, "dagger");

        var result = state.Apply(new PickUp(1, "dagger"), new FixedRandom());

        Assert.Equal(2, result.State.CurrentHunterId);
    }
}
