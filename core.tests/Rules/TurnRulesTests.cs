using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class TurnRulesTests
{
    [Fact]
    public void Given_NewGame_When_Started_Then_PhaseIsAwaitingRollAndRoundIsOne()
    {
        var (state, events) = Started(new FixedRandom());

        Assert.Equal(GamePhase.AwaitingRoll, state.Phase);
        Assert.Equal(1, state.Round);
        Assert.Contains(events, e => e is GameStarted);
        Assert.Contains(events, e => e is TurnStarted t && t.HunterId == state.CurrentHunterId);
    }

    [Fact]
    public void Given_StartedGame_When_StartedAgain_Then_Rejected()
    {
        var (state, _) = Started(new FixedRandom());

        var result = state.Apply(new StartGame(), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Same(state, result.State);
    }

    [Fact]
    public void Given_Seed_When_Started_Then_TurnOrderIsShuffledDeterministically()
    {
        var hunters = new[] { HunterAt(1, 0, 0), HunterAt(2, 4, 0), HunterAt(3, 0, 4), HunterAt(4, 4, 3) };

        var (a, _) = Started(new SeededRandom(99), hunters);
        var (b, _) = Started(new SeededRandom(99), hunters);

        Assert.Equal(a.TurnOrder, b.TurnOrder);
        Assert.Equal(new[] { 1, 2, 3, 4 }, a.TurnOrder.OrderBy(id => id));
    }

    [Fact]
    public void Given_AwaitingRoll_When_CurrentHunterRolls_Then_ActionPointsAreDiePlusSpeed()
    {
        var fast = HunterStats.Base with { Speed = 2 };
        var (state, _) = Started(new FixedRandom(), HunterAt(1, 0, 0, fast), HunterAt(2, 4, 0));

        var result = state.Apply(new RollDice(1), new FixedRandom(4));

        Assert.Equal(6, result.State.ActionPoints);
        Assert.Equal(GamePhase.Acting, result.State.Phase);
        var rolled = Assert.Single(result.Events.OfType<DiceRolled>());
        Assert.Equal((1, 4, 2, 6), (rolled.HunterId, rolled.Die, rolled.SpeedBonus, rolled.ActionPoints));
    }

    [Fact]
    public void Given_AwaitingRoll_When_OtherHunterRolls_Then_Rejected()
    {
        var (state, _) = Started(new FixedRandom());
        var other = state.TurnOrder[1];

        var result = state.Apply(new RollDice(other), new FixedRandom(6));

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(GamePhase.AwaitingRoll, result.State.Phase);
    }

    [Fact]
    public void Given_Acting_When_RollsAgain_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(3));

        var result = state.Apply(new RollDice(state.CurrentHunterId), new FixedRandom(6));

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(3, result.State.ActionPoints);
    }

    [Fact]
    public void Given_Acting_When_Passes_Then_ActionPointsAreZeroAndNextHunterIsUp()
    {
        var state = Rolled(new FixedRandom(5));
        var first = state.CurrentHunterId;
        var second = state.TurnOrder[1];

        var result = state.Apply(new Pass(first), new FixedRandom());

        Assert.Equal(0, result.State.ActionPoints);
        Assert.Equal(second, result.State.CurrentHunterId);
        Assert.Equal(GamePhase.AwaitingRoll, result.State.Phase);
        Assert.Contains(result.Events, e => e is TurnEnded t && t.HunterId == first);
        Assert.Contains(result.Events, e => e is TurnStarted t && t.HunterId == second);
    }

    [Fact]
    public void Given_AwaitingRoll_When_Passes_Then_Rejected()
    {
        var (state, _) = Started(new FixedRandom());

        var result = state.Apply(new Pass(state.CurrentHunterId), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_LastHunterOfRound_When_Passes_Then_RoundAdvances()
    {
        var random = new FixedRandom(1, 1);
        var state = Rolled(random);
        state = state.Apply(new Pass(state.CurrentHunterId), random).State;
        state = state.Apply(new RollDice(state.CurrentHunterId), random).State;

        var result = state.Apply(new Pass(state.CurrentHunterId), random);

        Assert.Equal(2, result.State.Round);
        Assert.Equal(result.State.TurnOrder[0], result.State.CurrentHunterId);
    }

    [Fact]
    public void Given_HunterExited_When_TurnOrderAdvances_Then_ExitedHunterIsSkipped()
    {
        // Caçador 1 começa na saída; caçador 2 e 3 em outro lugar. Ordem fixa 1,2,3 (FixedRandom mantém a ordem).
        var random = new FixedRandom(2, 2, 2);
        var state = Rolled(random, HunterAt(1, 4, 4), HunterAt(2, 0, 0), HunterAt(3, 4, 0));
        state = state.Apply(new Exit(1), random).State;           // 1 sai; turno vai para 2
        state = state.Apply(new RollDice(2), random).State;
        state = state.Apply(new Pass(2), random).State;           // turno vai para 3
        state = state.Apply(new RollDice(3), random).State;

        var result = state.Apply(new Pass(3), random);           // fim da rodada: deve pular o 1

        Assert.Equal(2, result.State.CurrentHunterId);
        Assert.Equal(2, result.State.Round);
    }

    [Fact]
    public void Given_Finished_When_AnyAction_Then_Rejected()
    {
        var config = new GameConfig(MaxRounds: 1);
        var random = new FixedRandom(1, 1);
        var state = NewGame(config, MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 0));
        state = state.Apply(new StartGame(), random).State;
        state = state.Apply(new RollDice(1), random).State;
        state = state.Apply(new Pass(1), random).State;
        state = state.Apply(new RollDice(2), random).State;
        state = state.Apply(new Pass(2), random).State;
        Assert.Equal(GamePhase.Finished, state.Phase);

        var result = state.Apply(new RollDice(1), random);

        Assert.Single(result.Events, e => e is ActionRejected);
    }
}
