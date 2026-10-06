using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class ReducerDeterminismTests
{
    [Fact]
    public void Given_SameSeedAndActions_When_Replayed_Then_StatesAreIdentical()
    {
        var a = Play(new SeededRandom(2024));
        var b = Play(new SeededRandom(2024));

        Assert.Equal(a.Round, b.Round);
        Assert.Equal(a.TurnOrder, b.TurnOrder);
        Assert.Equal(a.CurrentHunterId, b.CurrentHunterId);
        Assert.Equal(a.ActionPoints, b.ActionPoints);
        Assert.Equal(a.Hunters.Select(h => h.Position), b.Hunters.Select(h => h.Position));
    }

    [Fact]
    public void Given_Reducer_When_Applied_Then_OriginalStateIsUntouched()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));
        var before = state.Hunter(1).Position;

        _ = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Equal(before, state.Hunter(1).Position);
        Assert.Equal(4, state.ActionPoints);
    }

    private static GameState Play(IRandom random)
    {
        var hunters = new[] { HunterAt(1, 0, 0), HunterAt(2, 4, 0), HunterAt(3, 0, 4), HunterAt(4, 4, 3) };
        var state = NewGame(MapBuilder.Open5x5(), hunters);
        state = state.Apply(new StartGame(), random).State;

        // 3 rodadas: cada caçador rola, tenta andar 1 célula para o centro e passa.
        for (var i = 0; i < 12 && state.Phase != GamePhase.Finished; i++)
        {
            var id = state.CurrentHunterId;
            state = state.Apply(new RollDice(id), random).State;
            var pos = state.Hunter(id).Position;
            var target = new Position(pos.X + Math.Sign(2 - pos.X), pos.Y);
            state = state.Apply(new MoveTo(id, target), random).State;
            if (state.Phase == GamePhase.Acting && state.CurrentHunterId == id)
                state = state.Apply(new Pass(id), random).State;
        }

        return state;
    }
}
