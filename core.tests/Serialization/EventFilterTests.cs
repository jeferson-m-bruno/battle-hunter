using BattleHunter.Core.Rules;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Serialization;

public class EventFilterTests
{
    private const string Treasure = "treasure_dragon_eye";

    private static IReadOnlyList<GameEvent> Filter(IReadOnlyList<GameEvent> events, GameState state, int recipient) =>
        EventFilter.ForRecipient(events, state, recipient, TestContent.Default);

    [Fact]
    public void Given_CardDrawn_When_Filtered_Then_OnlyTheDrawerSeesIt()
    {
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(new Position(1, 0), false, false) }, Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 4))
            .StartAndRoll(new FixedRandom(4));
        var result = state.Apply(new OpenChest(1, new Position(1, 0)), new SeededRandom(1));

        Assert.Contains(Filter(result.Events, result.State, 1), e => e is CardDrawn);
        Assert.DoesNotContain(Filter(result.Events, result.State, 2), e => e is CardDrawn);
        Assert.Contains(Filter(result.Events, result.State, 2), e => e is ChestOpened);
    }

    [Fact]
    public void Given_HunterMovesInFog_When_Filtered_Then_OthersDoNotSeeIt()
    {
        // Caçador 1 anda em (0,0)→(1,0); caçador 2 em (4,4) não vê essa região.
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 4));
        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Contains(Filter(result.Events, result.State, 1), e => e is HunterMoved);
        Assert.DoesNotContain(Filter(result.Events, result.State, 2), e => e is HunterMoved);
    }

    [Fact]
    public void Given_MarkedHunterMovesInFog_When_Filtered_Then_EveryoneSeesIt()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0, hand: new[] { Treasure }), HunterAt(2, 4, 4))
            .StartAndRoll(new FixedRandom(4));
        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Contains(Filter(result.Events, result.State, 2), e => e is HunterMoved);
    }

    [Fact]
    public void Given_CardStolen_When_Filtered_Then_BystanderGetsRedactedCardId()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 1, 0, hand: new[] { "dagger" }), HunterAt(3, 4, 4))
            .StartAndRoll(new FixedRandom(5));
        var result = state.Apply(new Attack(1, new Position(1, 0)), new FixedRandom(3, 1, 1, 4));
        Assert.Contains(result.Events, e => e is CardStolen);

        Assert.Equal("dagger", Filter(result.Events, result.State, 1).OfType<CardStolen>().Single().CardId);
        Assert.Equal("dagger", Filter(result.Events, result.State, 2).OfType<CardStolen>().Single().CardId);
        Assert.Equal("", Filter(result.Events, result.State, 3).OfType<CardStolen>().Single().CardId);
    }

    [Fact]
    public void Given_TrapPlaced_When_Filtered_Then_OnlyOwnerSeesIt()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "trap_pit" }), HunterAt(2, 1, 0));
        var result = state.Apply(new UseCard(1, "trap_pit"), new FixedRandom());

        Assert.Contains(Filter(result.Events, result.State, 1), e => e is TrapPlaced);
        Assert.DoesNotContain(Filter(result.Events, result.State, 2), e => e is TrapPlaced);
    }

    [Fact]
    public void Given_PublicEvents_When_Filtered_Then_EveryoneGetsThem()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 4));
        var result = state.Apply(new Pass(1), new FixedRandom());

        foreach (var id in new[] { 1, 2 })
        {
            var mine = Filter(result.Events, result.State, id);
            Assert.Contains(mine, e => e is TurnEnded);
            Assert.Contains(mine, e => e is TurnStarted);
        }
    }
}
