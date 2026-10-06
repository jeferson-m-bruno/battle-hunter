using System.Text.Json;
using System.Text.Json.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.State;

public class GameStateSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public void Given_StartedGame_When_RoundTrippedThroughJson_Then_StateIsEqual()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var json = JsonSerializer.Serialize(state, Options);
        var back = JsonSerializer.Deserialize<GameState>(json, Options)!;

        Assert.Equal(state.Round, back.Round);
        Assert.Equal(state.Phase, back.Phase);
        Assert.Equal(state.ActionPoints, back.ActionPoints);
        Assert.Equal(state.TurnOrder, back.TurnOrder);
        Assert.Equal(state.Hunters, back.Hunters);
        Assert.Equal(state.Map.Width, back.Map.Width);
        Assert.Equal(state.Map.Cells, back.Map.Cells);
        Assert.Equal(json, JsonSerializer.Serialize(back, Options));
    }
}
