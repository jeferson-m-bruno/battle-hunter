using System.Text.Json;
using System.Text.Json.Serialization;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
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
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "dagger" }, equipment: new Equipment(Armor: "leather")), HunterAt(2, 4, 0));

        var json = JsonSerializer.Serialize(state, Options);
        var back = JsonSerializer.Deserialize<GameState>(json, Options)!;

        Assert.Equal(state.Round, back.Round);
        Assert.Equal(state.Phase, back.Phase);
        Assert.Equal(state.ActionPoints, back.ActionPoints);
        Assert.Equal(state.TurnOrder, back.TurnOrder);
        Assert.Equal(state.Hunter(1).Hand, back.Hunter(1).Hand);
        Assert.Equal(state.Hunter(1).Equipment, back.Hunter(1).Equipment);
        Assert.Equal(state.Map.Width, back.Map.Width);
        Assert.Equal(state.Map.Cells, back.Map.Cells);
        Assert.Equal(json, JsonSerializer.Serialize(back, Options));
    }

    [Fact]
    public void Given_GeneratedGame_When_RoundTrippedThroughJson_Then_MapRoomsAndChestsSurvive()
    {
        var random = new SeededRandom(5);
        var generated = MapGenerator.Generate(new MapSpec(), random);
        var hunters = Enumerable.Range(1, 4).Select(i => new HunterSetup(i, $"H{i}", HunterStats.Base)).ToList();
        var state = GameSetup.Create(new GameConfig(), generated, hunters, "treasure_dragon_eye");
        state = state.Apply(new StartGame(), random).State;

        var json = JsonSerializer.Serialize(state, Options);
        var back = JsonSerializer.Deserialize<GameState>(json, Options)!;

        Assert.Equal(state.Map.Rooms, back.Map.Rooms);
        Assert.Equal(state.Chests, back.Chests);
        Assert.Equal(state.TargetTreasureCardId, back.TargetTreasureCardId);
        Assert.Equal(json, JsonSerializer.Serialize(back, Options));
    }
}
