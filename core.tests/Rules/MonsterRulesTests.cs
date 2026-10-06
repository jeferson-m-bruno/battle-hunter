using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class MonsterRulesTests
{
    /// <summary>Passa o turno de todos os caçadores da rodada; a fase dos monstros roda no último.</summary>
    private static ReducerResult EndRound(GameState state, IRandom random)
    {
        ReducerResult result = new(state, Array.Empty<GameEvent>());
        var count = state.TurnOrder.Count;
        for (var i = 0; i < count; i++)
        {
            if (result.State.Phase == GamePhase.AwaitingRoll)
                result = result.State.Apply(new RollDice(result.State.CurrentHunterId), random);
            result = result.State.Apply(new Pass(result.State.CurrentHunterId), random);
        }

        return result;
    }

    [Fact]
    public void Given_MonsterWithLineOfSight_When_RoundEnds_Then_MovesTowardNearestHunterUpToD6()
    {
        // Kobold em (4,0), caçador 1 em (0,0); dado de movimento 2 -> vai para (2,0).
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 0, 4))
            .WithMonsters(MonsterAt(1, "kobold", 4, 0));
        var random = new FixedRandom(1, 1, 2);
        state = state.Apply(new StartGame(), random).State;

        var result = EndRound(state, random);

        Assert.Equal(new Position(2, 0), result.State.Monster(1).Position);
        var moved = Assert.Single(result.Events.OfType<MonsterMoved>());
        Assert.Equal((new Position(4, 0), new Position(2, 0)), (moved.From, moved.To));
        Assert.Contains(result.Events, e => e is MonsterPhaseEnded);
        Assert.Equal(2, result.State.Round);
    }

    [Fact]
    public void Given_MonsterAdjacent_When_RoundEnds_Then_AttacksInsteadOfMoving()
    {
        // Kobold em (1,0) adjacente ao caçador 1 (0,0): ATQ 3 + 4 = 7 vs DEF 2 + 1 = 3 -> 4 de dano.
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 0, 4))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0));
        var random = new FixedRandom(1, 1, 4, 1);
        state = state.Apply(new StartGame(), random).State;

        var result = EndRound(state, random);

        var attack = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.Equal(CombatantKind.Monster, attack.Attacker.Kind);
        Assert.Equal(4, attack.Damage);
        Assert.Equal(16, result.State.Hunter(1).Hp);
        Assert.Equal(new Position(1, 0), result.State.Monster(1).Position);
    }

    [Fact]
    public void Given_WallBlocksSight_When_RoundEnds_Then_MonsterStaysIdle()
    {
        var map = MapBuilder.FromAscii(
            "..#..",
            "..#..",
            "..#..",
            ".....",
            "....E");
        var state = NewGame(map, HunterAt(1, 0, 0), HunterAt(2, 0, 1))
            .WithMonsters(MonsterAt(1, "kobold", 4, 0));
        var random = new FixedRandom(1, 1, 6);
        state = state.Apply(new StartGame(), random).State;

        var result = EndRound(state, random);

        Assert.Equal(new Position(4, 0), result.State.Monster(1).Position);
        Assert.Empty(result.Events.OfType<MonsterMoved>());
    }

    [Fact]
    public void Given_MonsterReachesHunter_When_Moving_Then_StopsAdjacentAndDoesNotAttackSameTurn()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 0, 4))
            .WithMonsters(MonsterAt(1, "kobold", 4, 0));
        var random = new FixedRandom(1, 1, 6);
        state = state.Apply(new StartGame(), random).State;

        var result = EndRound(state, random);

        Assert.Equal(new Position(1, 0), result.State.Monster(1).Position);
        Assert.Empty(result.Events.OfType<AttackResolved>());
    }

    [Fact]
    public void Given_SpawnInterval_When_RoundSixStarts_Then_OneMonsterSpawns()
    {
        var state = NewGame(new GameConfig(MonsterSpawnInterval: 5), MapBuilder.Open5x5(), HunterAt(1, 0, 0))
            with { MonsterSpawns = new[] { new Position(2, 2) } };
        var random = new SeededRandom(1);
        state = state.Apply(new StartGame(), random).State;

        for (var round = 1; round <= 4; round++)
        {
            state = EndRound(state, random).State;
            Assert.Empty(state.Monsters);
        }

        var result = EndRound(state, random);

        Assert.Equal(6, result.State.Round);
        var spawned = Assert.Single(result.Events.OfType<MonsterSpawned>());
        Assert.Equal(new Position(2, 2), spawned.Position);
        Assert.Contains(spawned.TypeId, new[] { "kobold", "skeleton", "spider", "orc" });
    }

    [Fact]
    public void Given_GeneratedMap_When_SetUp_Then_OneMonsterPerSpawn()
    {
        var random = new SeededRandom(21);
        var map = MapGenerator.Generate(new MapSpec(), random);
        var hunters = Enumerable.Range(1, 4).Select(i => new HunterSetup(i, $"H{i}", HunterStats.Base)).ToList();

        var state = GameSetup.Create(new GameConfig(), map, hunters, "treasure_kobold_fang", random, TestContent.Default);

        Assert.Equal(map.MonsterSpawns.Count, state.Monsters.Count);
        Assert.Equal(map.MonsterSpawns.OrderBy(p => p.Y).ThenBy(p => p.X), state.Monsters.Select(m => m.Position).OrderBy(p => p.Y).ThenBy(p => p.X));
        Assert.All(state.Monsters, m => Assert.True(TestContent.Default.Monsters.Get(m.TypeId).Spawns));
        Assert.Equal(state.Monsters.Count + 1, state.NextMonsterId);
    }

    [Fact]
    public void Given_MimicChance_When_SetUp_Then_SomeChestsAreMimicsButNeverTheTarget()
    {
        var mimics = 0;
        var chests = 0;
        for (var seed = 1; seed <= 50; seed++)
        {
            var random = new SeededRandom(seed);
            var map = MapGenerator.Generate(new MapSpec(), random);
            var state = GameSetup.Create(new GameConfig(MimicChancePercent: 15), map, new[] { new HunterSetup(1, "H", HunterStats.Base) }, "treasure_kobold_fang", random, TestContent.Default);

            Assert.False(state.Chests.Single(c => c.HoldsTargetTreasure).IsMimic);
            mimics += state.Chests.Count(c => c.IsMimic);
            chests += state.Chests.Count - 1;
        }

        Assert.InRange(mimics / (double)chests, 0.08, 0.22);
    }

    [Fact]
    public void Given_MimicChest_When_Opened_Then_BecomesMonsterAndAttacksOpener()
    {
        var state = NewGameWithChests(
                MapBuilder.WithChests5x5(),
                new[] { new Chest(new Position(1, 0), false, false, IsMimic: true) },
                "treasure_kobold_fang",
                HunterAt(1, 0, 0), HunterAt(2, 4, 3))
            .StartAndRoll(new FixedRandom(4));

        // Mímico ATQ 5 + 3 = 8 vs DEF 2 + 1 = 3 -> 5 de dano.
        var result = state.Apply(new OpenChest(1, new Position(1, 0)), new FixedRandom(3, 1));

        var mimic = Assert.Single(result.State.Monsters);
        Assert.Equal(("mimic", new Position(1, 0)), (mimic.TypeId, mimic.Position));
        Assert.Contains(result.Events, e => e is MonsterSpawned s && s.TypeId == "mimic");
        var attack = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.Equal(5, attack.Damage);
        Assert.Equal(15, result.State.Hunter(1).Hp);
        Assert.Empty(result.State.Hunter(1).Hand);
        Assert.Empty(result.Events.OfType<CardDrawn>());
        Assert.Equal(2, result.State.ActionPoints);
    }

    [Fact]
    public void Given_MonsterKillsLastHunter_When_RoundEnds_Then_GameEndsAllHuntersOut()
    {
        var hunter = HunterAt(1, 0, 0) with { Hp = 1 };
        var state = NewGame(MapBuilder.Open5x5(), hunter).WithMonsters(MonsterAt(1, "orc", 1, 0));
        var random = new FixedRandom(1, 6, 1);
        state = state.Apply(new StartGame(), random).State;

        var result = EndRound(state, random);

        Assert.Equal(GamePhase.Finished, result.State.Phase);
        Assert.Equal(GameEndReason.AllHuntersOut, result.State.EndReason);
        Assert.Contains(result.Events, e => e is HunterFell f && f.KilledBy.Kind == CombatantKind.Monster);
    }

    [Fact]
    public void Given_MonsterInTheWay_When_HunterMoves_Then_Rejected()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 3))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }
}
