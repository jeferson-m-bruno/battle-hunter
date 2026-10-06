using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Rules;

public class GameSetupTests
{
    private static GeneratedMap Map(int seed) => MapGenerator.Generate(new MapSpec(), new SeededRandom(seed));

    private static List<HunterSetup> FourHunters() =>
        Enumerable.Range(1, 4).Select(i => new HunterSetup(i, $"H{i}", HunterStats.Base)).ToList();

    [Fact]
    public void Given_GeneratedMap_When_Created_Then_HuntersStandOnSpawnsAndChestsMatchMap()
    {
        var map = Map(42);
        var random = new SeededRandom(42);

        var state = GameSetup.Create(new GameConfig(MimicChancePercent: 0), map, FourHunters(), "treasure_kobold_fang", random, TestContent.Default);

        Assert.Equal(map.HunterSpawns, state.Hunters.Select(h => h.Position));
        Assert.Equal(map.Chests.Count, state.Chests.Count);
        Assert.Single(state.Chests, c => c.HoldsTargetTreasure);
        Assert.Equal(map.TargetChest, state.Chests.Single(c => c.HoldsTargetTreasure).Position);
        Assert.Equal(GamePhase.NotStarted, state.Phase);
    }

    [Fact]
    public void Given_TwoHunters_When_Created_Then_UsesFirstTwoSpawns()
    {
        var map = Map(7);
        var random = new SeededRandom(7);

        var state = GameSetup.Create(new GameConfig(), map, FourHunters().Take(2).ToList(), "treasure_kobold_fang", random, TestContent.Default);

        Assert.Equal(2, state.Hunters.Count);
        Assert.Equal(map.HunterSpawns.Take(2), state.Hunters.Select(h => h.Position));
    }

    [Fact]
    public void Given_HandAboveFive_When_Created_Then_Throws()
    {
        var hunters = FourHunters();
        hunters[0] = hunters[0] with { Hand = Enumerable.Repeat("dagger", 6).ToList() };

        Assert.Throws<ArgumentException>(() => GameSetup.Create(new GameConfig(), Map(1), hunters, "treasure_kobold_fang", new SeededRandom(1), TestContent.Default));
    }

    [Fact]
    public void Given_FullGeneratedGame_When_HunterWalksToTreasureAndExit_Then_WinsMission()
    {
        // Partida completa sobre um mapa gerado: o caçador 1 anda até o baú-alvo, abre, anda até a saída e sai.
        var random = new SeededRandom(2026);
        var map = Map(2026);
        var state = GameSetup.Create(new GameConfig(MaxRounds: 200, MimicChancePercent: 0, MonsterSpawnInterval: 0), map, FourHunters().Take(1).ToList(), "treasure_lost_crown", random, TestContent.Default)
            .WithMonsters();
        state = state.Apply(new StartGame(), random).State;

        state = WalkTo(state, map.TargetChest, random, stopAdjacent: true);
        state = EnsureActing(state, random, minPa: 2);
        state = state.Apply(new OpenChest(1, map.TargetChest), random).State;
        Assert.True(state.IsMarked(1));

        state = WalkTo(state, map.Exit, random, stopAdjacent: false);
        state = EnsureActing(state, random, minPa: 1);
        var result = state.Apply(new Exit(1), random);

        Assert.Equal(GamePhase.Finished, result.State.Phase);
        Assert.Equal(GameEndReason.TreasureExtracted, result.State.EndReason);
        Assert.Equal(1, result.State.WinnerId);
        Assert.Contains(result.Events, e => e is GameEnded g && g.WinnerId == 1);
    }

    private static GameState EnsureActing(GameState state, IRandom random, int minPa)
    {
        while (state.Phase != GamePhase.Acting || state.ActionPoints < minPa)
        {
            if (state.Phase == GamePhase.AwaitingRoll)
                state = state.Apply(new RollDice(1), random).State;
            else
                state = state.Apply(new Pass(1), random).State;
        }

        return state;
    }

    private static GameState WalkTo(GameState state, Position target, IRandom random, bool stopAdjacent)
    {
        while (true)
        {
            var pos = state.Hunter(1).Position;
            if (stopAdjacent ? pos.IsOrthogonallyAdjacentTo(target) : pos == target)
                return state;

            state = EnsureActing(state, random, minPa: 1);
            var dist = Pathfinding.Distances(state.Map, target, p => state.Map.IsWalkable(p) || p == target);
            var next = Pathfinding.Neighbors(pos)
                .Where(n => state.Map.IsWalkable(n) && dist.ContainsKey(n))
                .OrderBy(n => dist[n])
                .First();
            state = state.Apply(new MoveTo(1, next), random).State;
        }
    }
}
