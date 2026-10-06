using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class TrapRulesTests
{
    private static GameState WithTrap(Trap trap, IRandom random, params Hunter[] hunters)
    {
        var hs = hunters.Length == 0 ? new[] { HunterAt(1, 0, 0), HunterAt(2, 4, 0) } : hunters;
        var state = NewGame(MapBuilder.Open5x5(), hs) with { Traps = new[] { trap } };
        return state.StartAndRoll(random);
    }

    [Fact]
    public void Given_TrapCard_When_Used_Then_PlacedOnCurrentCellForTwoPa()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "trap_pit" }), HunterAt(2, 4, 0));

        var result = state.Apply(new UseCard(1, "trap_pit"), new FixedRandom());

        var trap = Assert.Single(result.State.Traps);
        Assert.Equal((new Position(0, 0), "trap_pit", 1), (trap.Position, trap.CardId, trap.OwnerId));
        Assert.Equal(2, result.State.ActionPoints);
        Assert.Contains(result.Events, e => e is TrapPlaced p && p.OwnerId == 1);
    }

    [Fact]
    public void Given_TrapAlreadyOnCell_When_AnotherPlaced_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "trap_pit", "trap_net" }), HunterAt(2, 4, 0));
        state = state.Apply(new UseCard(1, "trap_pit"), new FixedRandom()).State;

        var result = state.Apply(new UseCard(1, "trap_net"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_PitOwnedByOther_When_HunterStepsIn_Then_TakesDamageAndTrapIsGone()
    {
        var state = WithTrap(new Trap(new Position(1, 0), "trap_pit", 2), new FixedRandom(4));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Equal(16, result.State.Hunter(1).Hp);
        Assert.Empty(result.State.Traps);
        Assert.Contains(result.Events, e => e is TrapTriggered t && t.CardId == "trap_pit" && t.Victim.Id == 1);
        Assert.Contains(result.Events, e => e is DamageDealt d && d.Damage == 4 && d.Cause == "trap_pit");
    }

    [Fact]
    public void Given_OwnTrap_When_OwnerStepsIn_Then_NothingHappens()
    {
        var state = WithTrap(new Trap(new Position(1, 0), "trap_pit", 1), new FixedRandom(4));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Equal(20, result.State.Hunter(1).Hp);
        Assert.Single(result.State.Traps);
    }

    [Fact]
    public void Given_Net_When_HunterStepsIn_Then_LosesNextTurn()
    {
        // Rede em (1,0), dono 2. Caçador 1 pisa, passa; rodada 2: o turno do 1 é pulado e o 2 joga.
        var random = new FixedRandom(4, 1, 1, 1);
        var state = WithTrap(new Trap(new Position(1, 0), "trap_net", 2), random);

        state = state.Apply(new MoveTo(1, new Position(1, 0)), random).State;
        Assert.True(state.Hunter(1).HasStatus(StatusKind.Trapped));

        state = state.Apply(new Pass(1), random).State;         // turno do 2
        state = state.Apply(new RollDice(2), random).State;
        var result = state.Apply(new Pass(2), random);          // rodada 2: 1 preso -> pula para o 2

        Assert.Contains(result.Events, e => e is TurnSkipped s && s.HunterId == 1);
        Assert.Equal(2, result.State.CurrentHunterId);
        Assert.Equal(2, result.State.Round);
        Assert.False(result.State.Hunter(1).HasStatus(StatusKind.Trapped));
    }

    [Fact]
    public void Given_PoisonSpikes_When_HunterStepsIn_Then_DamageAndPoisonedForThreeTurns()
    {
        var state = WithTrap(new Trap(new Position(1, 0), "trap_spikes", 2), new FixedRandom(4));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Equal(18, result.State.Hunter(1).Hp);
        var poison = Assert.Single(result.State.Hunter(1).Statuses);
        Assert.Equal((StatusKind.Poisoned, 3), (poison.Kind, poison.TurnsLeft));
    }

    [Fact]
    public void Given_Alarm_When_HunterStepsIn_Then_MonstersWithoutSightMoveTowardIt()
    {
        var map = MapBuilder.FromAscii(
            "..#..",
            "..#..",
            "..#..",
            ".....",
            "....E");
        var random = new FixedRandom(4, 3);
        var state = (NewGame(map, HunterAt(1, 0, 0)) with { Traps = new[] { new Trap(new Position(1, 0), "trap_alarm", 99) } })
            .WithMonsters(MonsterAt(1, "kobold", 4, 0))
            .StartAndRoll(random);

        state = state.Apply(new MoveTo(1, new Position(1, 0)), random).State;
        Assert.Equal(new Alarm(new Position(1, 0), 3), state.Alarm);

        var result = state.Apply(new Pass(1), random);

        var moved = Assert.Single(result.Events.OfType<MonsterMoved>());
        Assert.Equal(new Position(4, 0), moved.From);
        Assert.True(moved.To.DistanceTo(new Position(1, 0)) < 5);
        Assert.Equal(2, result.State.Alarm!.RoundsLeft);
    }

    [Fact]
    public void Given_Pit_When_MonsterWalksOver_Then_MonsterTakesDamage()
    {
        // Kobold em (4,0) anda 6 rumo ao caçador em (0,0): pisa no Fosso em (2,0) e para adjacente em (1,0).
        var random = new FixedRandom(1, 6);
        var state = (NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0)) with { Traps = new[] { new Trap(new Position(2, 0), "trap_pit", 1) } })
            .WithMonsters(MonsterAt(1, "kobold", 4, 0))
            .StartAndRoll(random);

        var result = state.Apply(new Pass(1), random);

        Assert.Equal(4, result.State.Monster(1).Hp);
        Assert.Equal(new Position(1, 0), result.State.Monster(1).Position);
        Assert.Empty(result.State.Traps);
        Assert.Contains(result.Events, e => e is TrapTriggered t && t.Victim.Kind == CombatantKind.Monster);
    }

    [Fact]
    public void Given_BearTrap_When_MonsterWalksOver_Then_SkipsNextPhase()
    {
        var random = new FixedRandom(1, 6, 1, 6);
        var state = (NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0)) with { Traps = new[] { new Trap(new Position(3, 0), "trap_bear", 1) } })
            .WithMonsters(MonsterAt(1, "kobold", 4, 0))
            .StartAndRoll(random);

        state = state.Apply(new Pass(1), random).State;
        Assert.True(state.Monster(1).SkipsNextAction);
        Assert.Equal(3, state.Monster(1).Hp);

        state = state.Apply(new RollDice(1), random).State;
        var result = state.Apply(new Pass(1), random);

        Assert.Empty(result.Events.OfType<MonsterMoved>());
        Assert.False(result.State.Monster(1).SkipsNextAction);
    }
}
