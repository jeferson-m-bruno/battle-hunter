using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class EndConditionsTests
{
    [Fact]
    public void Given_OnExitCell_When_Exits_Then_HunterStatusIsExitedAndTurnEnds()
    {
        var random = new FixedRandom(3);
        var state = Rolled(random, HunterAt(1, 4, 4), HunterAt(2, 0, 0));

        var result = state.Apply(new Exit(1), random);

        Assert.Equal(HunterStatus.Exited, result.State.Hunter(1).Status);
        Assert.Contains(result.Events, e => e is HunterExited h && h.HunterId == 1);
        Assert.Contains(result.Events, e => e is TurnEnded t && t.HunterId == 1);
        Assert.Equal(2, result.State.CurrentHunterId);
    }

    [Fact]
    public void Given_NotOnExitCell_When_Exits_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(3), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new Exit(1), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Equal(HunterStatus.Active, result.State.Hunter(1).Status);
    }

    [Fact]
    public void Given_AllHuntersExited_When_LastExits_Then_GameEndsAllHuntersOut()
    {
        var random = new FixedRandom(3, 3);
        var state = Rolled(random, HunterAt(1, 4, 4), HunterAt(2, 3, 4));
        state = state.Apply(new Exit(1), random).State;
        state = state.Apply(new RollDice(2), random).State;
        state = state.Apply(new MoveTo(2, new Position(4, 4)), random).State;

        var result = state.Apply(new Exit(2), random);

        Assert.Equal(GamePhase.Finished, result.State.Phase);
        Assert.Equal(GameEndReason.AllHuntersOut, result.State.EndReason);
        Assert.Contains(result.Events, e => e is GameEnded g && g.Reason == GameEndReason.AllHuntersOut);
    }

    [Fact]
    public void Given_RoundLimit_When_LastRoundEnds_Then_GameEndsRoundLimit()
    {
        var config = new GameConfig(MaxRounds: 2);
        var random = new FixedRandom(1, 1, 1, 1);
        var state = NewGame(config, MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 0));
        state = state.Apply(new StartGame(), random).State;

        for (var i = 0; i < 3; i++)
        {
            state = state.Apply(new RollDice(state.CurrentHunterId), random).State;
            state = state.Apply(new Pass(state.CurrentHunterId), random).State;
            Assert.Equal(GamePhase.AwaitingRoll, state.Phase);
        }

        state = state.Apply(new RollDice(state.CurrentHunterId), random).State;
        var result = state.Apply(new Pass(state.CurrentHunterId), random);

        Assert.Equal(GamePhase.Finished, result.State.Phase);
        Assert.Equal(GameEndReason.RoundLimit, result.State.EndReason);
        Assert.Equal(2, result.State.Round);
    }

    [Fact]
    public void Given_DefaultConfig_When_Created_Then_MaxRoundsIsThirty()
    {
        Assert.Equal(30, new GameConfig().MaxRounds);
    }
}
