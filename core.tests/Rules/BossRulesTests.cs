using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class BossRulesTests
{
    private const string Treasure = "treasure_dragon_eye";
    private static readonly Position Exit = new(4, 4);

    /// <summary>Passa uma rodada inteira (1 caçador): rola e passa.</summary>
    private static ReducerResult EndRound(GameState state, IRandom random)
    {
        var rolled = state.Phase == GamePhase.AwaitingRoll ? state.Apply(new RollDice(state.CurrentHunterId), random).State : state;
        return rolled.Apply(new Pass(rolled.CurrentHunterId), random);
    }

    [Fact]
    public void Given_BossRound_When_RoundStarts_Then_DragonAppearsNearExitOnce()
    {
        var random = new SeededRandom(3);
        var state = NewGame(new GameConfig(BossRound: 3, MonsterSpawnInterval: 0), MapBuilder.Open5x5(), HunterAt(1, 0, 0))
            .Apply(new StartGame(), random).State;

        state = EndRound(state, random).State;                 // rodada 2
        Assert.Empty(state.Monsters);

        var result = EndRound(state, random);                  // rodada 3: chefe
        var boss = Assert.Single(result.State.Monsters);
        Assert.Equal("dragon", boss.TypeId);
        Assert.Equal(60, boss.Hp);
        Assert.True(boss.Position.DistanceTo(Exit) <= 2);
        Assert.Contains(result.Events, e => e is BossAppeared b && b.MonsterId == boss.Id);
        Assert.True(result.State.BossSpawned);

        var later = EndRound(result.State, random);            // rodada 4: não repete
        Assert.Single(later.State.Monsters, m => m.TypeId == "dragon");
    }

    [Fact]
    public void Given_TreasurePickedBeforeBossRound_When_ChestOpened_Then_DragonAppearsImmediately()
    {
        var state = NewGameWithChests(
                MapBuilder.WithChests5x5(),
                new[] { new Chest(new Position(1, 0), false, true) },
                Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 0))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new OpenChest(1, new Position(1, 0)), new SeededRandom(1));

        Assert.True(result.State.IsMarked(1));
        Assert.Single(result.State.Monsters, m => m.TypeId == "dragon");
        Assert.Contains(result.Events, e => e is BossAppeared);
    }

    [Fact]
    public void Given_MissionWithoutBoss_When_TreasurePickedAndRoundsPass_Then_NoDragon()
    {
        var random = new SeededRandom(3);
        var state = NewGameWithChests(
                MapBuilder.WithChests5x5(),
                new[] { new Chest(new Position(1, 0), false, true) },
                Treasure,
                HunterAt(1, 0, 0))
            with { Config = new GameConfig(BossRound: 0, MonsterSpawnInterval: 0) };
        state = state.StartAndRoll(new FixedRandom(4));
        state = state.Apply(new OpenChest(1, new Position(1, 0)), random).State;

        for (var i = 0; i < 20; i++)
            state = EndRound(state, random).State;

        Assert.Empty(state.Monsters);
        Assert.False(state.BossSpawned);
    }

    [Fact]
    public void Given_HardMission_When_BossAppears_Then_HpIsOneAndAHalf()
    {
        var random = new SeededRandom(3);
        var state = NewGame(new GameConfig(BossRound: 2, BossHpPercent: 150, MonsterSpawnInterval: 0), MapBuilder.Open5x5(), HunterAt(1, 0, 0))
            .Apply(new StartGame(), random).State;

        var result = EndRound(state, random);

        Assert.Equal(90, result.State.Monsters.Single().Hp);
    }

    [Fact]
    public void Given_ThirdAction_When_BossActs_Then_BreathesLineOfThreeIgnoringDefense()
    {
        // Dragão em (2,3) com 2 ações feitas; caçador 1 ao norte em (2,1) com DEF 5 (Placas). Sopro: (2,2),(2,1),(2,0).
        var random = new FixedRandom(1);
        var hunter = HunterAt(1, 2, 1, equipment: new Equipment(Armor: "plate"));
        var state = NewGame(new GameConfig(BossRound: 0), MapBuilder.Open5x5(), hunter, HunterAt(2, 0, 0))
            .WithMonsters(MonsterAt(1, "dragon", 2, 3) with { ActionsTaken = 2 })
            .StartAndRoll(random);
        state = state.Apply(new Pass(1), random).State;
        state = state.Apply(new RollDice(2), random).State;

        var result = state.Apply(new Pass(2), random);

        var breath = Assert.Single(result.Events.OfType<BossBreath>());
        Assert.Equal(new[] { new Position(2, 2), new Position(2, 1), new Position(2, 0) }, breath.Cells);
        Assert.Equal(12, result.State.Hunter(1).Hp);
        Assert.Equal(20, result.State.Hunter(2).Hp);
        Assert.Contains(result.Events, e => e is DamageDealt d && d.Cause == "breath" && d.Damage == 8);
        Assert.Empty(result.Events.OfType<AttackResolved>());
        Assert.Equal(new Position(2, 3), result.State.Monster(1).Position);
    }

    [Fact]
    public void Given_WallInTheWay_When_BossBreathes_Then_LineStopsAtWall()
    {
        var map = MapBuilder.FromAscii(
            ".....",
            "..#..",
            ".....",
            ".....",
            "....E");
        var random = new FixedRandom(1);
        var state = NewGame(new GameConfig(BossRound: 0), MapBuilder.Open5x5() with { Cells = map.Cells }, HunterAt(1, 2, 0))
            .WithMonsters(MonsterAt(1, "dragon", 2, 3) with { ActionsTaken = 2 })
            .StartAndRoll(random);

        var result = state.Apply(new Pass(1), random);

        var breath = Assert.Single(result.Events.OfType<BossBreath>());
        Assert.Equal(new[] { new Position(2, 2) }, breath.Cells);
        Assert.Equal(20, result.State.Hunter(1).Hp);
    }

    [Fact]
    public void Given_NotThirdAction_When_BossActs_Then_NormalChaseAndAttack()
    {
        // Dragão adjacente, 1ª ação: ataque normal ATQ 10 + 2 vs DEF 2 + 1 -> 9.
        var random = new FixedRandom(1, 2, 1);
        var state = NewGame(new GameConfig(BossRound: 0), MapBuilder.Open5x5(), HunterAt(1, 0, 0))
            .WithMonsters(MonsterAt(1, "dragon", 1, 0))
            .StartAndRoll(random);

        var result = state.Apply(new Pass(1), random);

        Assert.Equal(9, result.Events.OfType<AttackResolved>().Single().Damage);
        Assert.Empty(result.Events.OfType<BossBreath>());
        Assert.Equal(1, result.State.Monster(1).ActionsTaken);
    }

    [Fact]
    public void Given_BossDefeated_When_Resolved_Then_150XpAndGuaranteedRareCard()
    {
        var state = NewGame(new GameConfig(BossRound: 0), MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 3))
            .WithMonsters(MonsterAt(1, "dragon", 1, 0, hp: 1))
            .StartAndRoll(new FixedRandom(4));

        // Dados: ataque 1, defesa 1; sem rolagem de sorte para o loot (garantido) -> o próximo dado só serviria ao sorteio.
        var result = state.Apply(new Attack(1, new Position(1, 0)), new FixedRandom(1, 1, 6));

        Assert.Empty(result.State.Monsters);
        Assert.Equal(150, result.State.Hunter(1).Xp);
        var drawn = Assert.Single(result.Events.OfType<CardDrawn>());
        Assert.Equal(Core.Cards.Rarity.Rare, TestContent.Default.Cards.Get(drawn.CardId).Rarity);
    }
}
