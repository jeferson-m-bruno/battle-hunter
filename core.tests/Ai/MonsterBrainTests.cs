using BattleHunter.Core.Ai;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Ai;

public class MonsterBrainTests
{
    /// <summary>Grid 8×8 aberto com uma sala (0..3, 0..3) registrada; o resto é corredor.</summary>
    private static GridMap RoomMap()
    {
        var cells = Enumerable.Repeat(Cell.Floor, 64).ToList();
        cells[7 * 8 + 7] = Cell.Exit;
        return new GridMap(8, 8, cells, new[] { new Room(0, 0, 4, 4) });
    }

    [Fact]
    public void Given_NoHunterInSight_When_Decided_Then_Idle()
    {
        var map = MapBuilder.FromAscii(
            "..#..",
            "..#..",
            "..#..",
            ".....",
            "....E");
        var state = NewGame(map, HunterAt(1, 0, 0)).WithMonsters(MonsterAt(1, "kobold", 4, 0));

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(MonsterState.Idle, decision.State);
    }

    [Fact]
    public void Given_HunterInSightNotAdjacent_When_Decided_Then_ChasingTheNearest()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 3, 2)).WithMonsters(MonsterAt(1, "kobold", 4, 0));

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(MonsterState.Chasing, decision.State);
        Assert.Equal(2, decision.TargetHunterId);
        Assert.Equal(new Position(3, 2), decision.Destination);
    }

    [Fact]
    public void Given_HunterAdjacent_When_Decided_Then_Attacking()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 3, 0)).WithMonsters(MonsterAt(1, "kobold", 4, 0));

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(MonsterState.Attacking, decision.State);
        Assert.Equal(1, decision.TargetHunterId);
    }

    [Fact]
    public void Given_Orc_When_TwoHuntersVisible_Then_ChasesTheMostWounded()
    {
        var near = HunterAt(1, 3, 0);
        var wounded = HunterAt(2, 0, 4) with { Hp = 5 };
        var state = NewGame(MapBuilder.Open5x5(), near, wounded).WithMonsters(MonsterAt(1, "orc", 4, 0));

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(2, decision.TargetHunterId);
    }

    [Fact]
    public void Given_Skeleton_When_HunterOutsideItsRoom_Then_StaysIdle()
    {
        var state = NewGame(RoomMap(), HunterAt(1, 6, 1)).WithMonsters(MonsterAt(1, "skeleton", 1, 1));

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(MonsterState.Idle, decision.State);
    }

    [Fact]
    public void Given_Skeleton_When_HunterEntersRoom_Then_ChasesWithinRoomOnly()
    {
        var state = NewGame(RoomMap(), HunterAt(1, 3, 3)).WithMonsters(MonsterAt(1, "skeleton", 0, 0));

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(MonsterState.Chasing, decision.State);
        Assert.NotNull(decision.IsPassable);
        Assert.True(decision.IsPassable!(new Position(2, 2)));
        Assert.False(decision.IsPassable!(new Position(5, 5)));
    }

    [Fact]
    public void Given_Skeleton_When_HundredPhasesPass_Then_NeverLeavesRoom()
    {
        var random = new SeededRandom(11);
        var room = new Room(0, 0, 4, 4);
        var state = NewGame(new GameConfig(MaxRounds: 500, MonsterSpawnInterval: 0, BossRound: 0), RoomMap(), HunterAt(1, 6, 6))
            .WithMonsters(MonsterAt(1, "skeleton", 1, 1))
            .Apply(new StartGame(), random).State;

        for (var i = 0; i < 100; i++)
        {
            state = state.Apply(new RollDice(1), random).State;
            state = state.Apply(new Pass(1), random).State;
            Assert.True(room.Contains(state.Monster(1).Position), $"fase {i}: esqueleto em {state.Monster(1).Position}");
        }
    }

    [Fact]
    public void Given_StateChanges_When_PhaseRuns_Then_EventsReportTransitions()
    {
        var random = new FixedRandom(1, 6, 1, 3, 1);
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0)).WithMonsters(MonsterAt(1, "kobold", 4, 0)).StartAndRoll(random);

        var first = state.Apply(new Pass(1), random);
        Assert.Contains(first.Events, e => e is MonsterStateChanged c && c.From == MonsterState.Idle && c.To == MonsterState.Chasing);
        Assert.Equal(MonsterState.Chasing, first.State.Monster(1).State);

        state = first.State.Apply(new RollDice(1), random).State;
        var second = state.Apply(new Pass(1), random);
        Assert.Contains(second.Events, e => e is MonsterStateChanged c && c.From == MonsterState.Chasing && c.To == MonsterState.Attacking);
        Assert.Single(second.Events.OfType<AttackResolved>());
    }

    [Fact]
    public void Given_AlarmAndNoSight_When_Decided_Then_ChasingTheAlarm()
    {
        var map = MapBuilder.FromAscii(
            "..#..",
            "..#..",
            "..#..",
            ".....",
            "....E");
        var state = NewGame(map, HunterAt(1, 0, 0)).WithMonsters(MonsterAt(1, "kobold", 4, 0)) with { Alarm = new Alarm(new Position(3, 3), 2) };

        var decision = MonsterBrain.Decide(state, state.Monster(1), TestContent.Default);

        Assert.Equal(MonsterState.Chasing, decision.State);
        Assert.Null(decision.TargetHunterId);
        Assert.Equal(new Position(3, 2), decision.Destination);
    }
}
