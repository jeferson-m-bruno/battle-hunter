using BattleHunter.Core.Ai;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Serialization;

public class MessageJsonTests
{
    public static IEnumerable<object[]> Actions => new[]
    {
        new object[] { new StartGame() },
        new object[] { new RollDice(1) },
        new object[] { new MoveTo(2, new Position(3, 4)) },
        new object[] { new Pass(1) },
        new object[] { new Exit(1) },
        new object[] { new OpenChest(1, new Position(1, 0)) },
        new object[] { new Discard(1, "dagger") },
        new object[] { new Equip(1, "axe") },
        new object[] { new Attack(1, new Position(0, 1)) },
        new object[] { new PickUp(1, "boots") },
        new object[] { new UseCard(1, "bomb", new Position(2, 2)) },
        new object[] { new UseCard(1, "potion") },
    };

    [Theory]
    [MemberData(nameof(Actions))]
    public void Given_Action_When_RoundTripped_Then_EqualAndCarriesTypeDiscriminator(GameAction action)
    {
        var json = MessageJson.Serialize(action);
        var back = MessageJson.Deserialize<GameAction>(json);

        Assert.Contains($"\"type\":\"{action.GetType().Name}\"", json);
        Assert.Equal(action, back);
    }

    [Fact]
    public void Given_Events_When_RoundTripped_Then_Equal()
    {
        var events = new GameEvent[]
        {
            new GameStarted(new[] { 2, 1 }),
            new DiceRolled(1, 4, 1, 5),
            new AttackResolved(Combatant.HunterRef(1), Combatant.MonsterRef(3), 6, 2, 14, true, false, 0),
            new HunterFell(2, Combatant.MonsterRef(1), new Position(1, 1), new[] { "dagger", "boots" }),
            new BossBreath(5, new[] { new Position(1, 1), new Position(1, 2) }),
            new ActionRejected(new MoveTo(1, new Position(9, 9)), "fora"),
            new GameEnded(GameEndReason.TreasureExtracted, 2),
        };

        var json = MessageJson.Serialize<IReadOnlyList<GameEvent>>(events);
        var back = MessageJson.Deserialize<List<GameEvent>>(json);

        Assert.Equal(events.Length, back.Count);
        for (var i = 0; i < events.Length; i++)
            Assert.Equal(events[i].GetType(), back[i].GetType());
        Assert.Equal(new[] { 2, 1 }, ((GameStarted)back[0]).TurnOrder);
        Assert.Equal(14, ((AttackResolved)back[2]).Damage);
        Assert.Equal(new[] { "dagger", "boots" }, ((HunterFell)back[3]).DroppedCards);
        Assert.Equal("fora", ((ActionRejected)back[5]).Reason);
    }

    [Fact]
    public void Given_UnknownType_When_Deserialized_Then_Throws()
    {
        Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => MessageJson.Deserialize<GameAction>("{\"type\":\"Hack\",\"hunterId\":1}"));
        Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => MessageJson.Deserialize<GameAction>("{\"hunterId\":1}"));
    }

    [Fact]
    public void Given_ClientAndServerMessages_When_RoundTripped_Then_Equal()
    {
        var c = MessageJson.Deserialize<ClientMessage>(MessageJson.Serialize<ClientMessage>(new PlayerAction(new MoveTo(1, new Position(1, 2)))));
        Assert.Equal(new MoveTo(1, new Position(1, 2)), ((PlayerAction)c).Action);

        var auth = MessageJson.Deserialize<ClientMessage>("{\"type\":\"Auth\",\"deviceId\":\"abc\",\"name\":\"Jef\"}");
        Assert.Equal(new Auth("abc", "Jef"), auth);

        var s = MessageJson.Deserialize<ServerMessage>(MessageJson.Serialize<ServerMessage>(new Rejected("nope")));
        Assert.Equal(new Rejected("nope"), s);
    }

    [Fact]
    public void Given_GeneratedGame_When_SnapshotSerialized_Then_RoundTripsAndHidesOtherHands()
    {
        var random = new SeededRandom(9);
        var map = MapGenerator.Generate(new MapSpec(), random);
        var hunters = Enumerable.Range(1, 4).Select(i => new HunterSetup(i, $"H{i}", HunterStats.Base, new[] { "dagger", "potion" })).ToList();
        var state = GameSetup.Create(new GameConfig(), map, hunters, "treasure_lost_crown", random, TestContent.Default);
        state = state.Apply(new StartGame(), random).State;

        var snapshot = PlayerSnapshot.For(state, 1, TestContent.Default);
        var json = MessageJson.Serialize<ServerMessage>(new MatchStarted("r1", 1, snapshot));
        var back = (MatchStarted)MessageJson.Deserialize<ServerMessage>(json);

        Assert.Equal(1, back.HunterId);
        Assert.Equal(2, back.Snapshot.Self.Hand.Count);
        Assert.Equal(map.Map.Cells, back.Snapshot.Map.Cells);
        Assert.Equal(4, back.Snapshot.Hunters.Count);
        Assert.All(back.Snapshot.Hunters.Where(h => h.Id != 1), h => Assert.Equal(2, h.HandCount));
        Assert.DoesNotContain("\"hand\":[\"dagger\",\"potion\"]", json.Replace(MessageJson.Serialize(snapshot.Self), ""));
        Assert.Equal(MessageJson.Serialize<ServerMessage>(back), json);
    }

    [Fact]
    public void Given_Snapshot_When_OtherHunterInFog_Then_PositionNullUnlessMarked()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), "treasure_dragon_eye",
                HunterAt(1, 0, 0), HunterAt(2, 4, 4), HunterAt(3, 4, 0, hand: new[] { "treasure_dragon_eye" }))
            .StartAndRoll(new FixedRandom(4));

        var snapshot = PlayerSnapshot.For(state, 1, TestContent.Default);

        Assert.Null(snapshot.Hunters.Single(h => h.Id == 2).Position);
        Assert.Equal(new Position(4, 0), snapshot.Hunters.Single(h => h.Id == 3).Position);
        Assert.Equal(3, snapshot.MarkedHunterId);
        Assert.Equal(1, snapshot.CurrentHunterId);
    }
}
