using BattleHunter.Core.Cards;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class ChestRulesTests
{
    private const string Treasure = "treasure_dragon_eye";
    private static readonly Position ChestA = new(1, 0);
    private static readonly Position ChestB = new(3, 0);

    /// <summary>Caçador 1 em (0,0) ao lado do baú A; caçador 2 em (4,0) ao lado do baú B (tesouro).</summary>
    private static GameState Game(IRandom random, IReadOnlyList<string>? hand = null) =>
        NewGameWithChests(
                MapBuilder.WithChests5x5(),
                new[] { new Chest(ChestA, false, false), new Chest(ChestB, false, true) },
                Treasure,
                HunterAt(1, 0, 0, hand: hand), HunterAt(2, 4, 0))
            .StartAndRoll(random);

    [Fact]
    public void Given_AdjacentChest_When_Opened_Then_CostsTwoPaAndCardGoesToHand()
    {
        var state = Game(new FixedRandom(4));

        var result = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));

        Assert.Equal(2, result.State.ActionPoints);
        Assert.Single(result.State.Hunter(1).Hand);
        Assert.True(result.State.ChestAt(ChestA)!.IsOpened);
        var drawn = Assert.Single(result.Events.OfType<CardDrawn>());
        Assert.True(TestContent.Default.Cards.Contains(drawn.CardId));
        Assert.Contains(result.Events, e => e is ChestOpened c && c.ChestPosition == ChestA);
    }

    [Fact]
    public void Given_OnePa_When_Opened_Then_Rejected()
    {
        var state = Game(new FixedRandom(1));

        var result = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Empty(result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_ChestNotAdjacent_When_Opened_Then_Rejected()
    {
        var state = Game(new FixedRandom(4));

        var result = state.Apply(new OpenChest(1, ChestB), new SeededRandom(1));

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_AlreadyOpened_When_OpenedAgain_Then_Rejected()
    {
        var state = Game(new FixedRandom(6));
        state = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1)).State;

        var result = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_FullHand_When_Opened_Then_RejectedUntilDiscard()
    {
        var hand = Enumerable.Repeat("dagger", GameState.MaxHandSize).ToList();
        var state = Game(new FixedRandom(4), hand);

        var rejected = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));
        Assert.Single(rejected.Events, e => e is ActionRejected);

        state = state.Apply(new Discard(1, "dagger"), new SeededRandom(1)).State;
        Assert.Equal(9, state.Hunter(1).Hand.Count);
        Assert.Equal(4, state.ActionPoints);

        var opened = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));
        Assert.Equal(10, opened.State.Hunter(1).Hand.Count);
    }

    [Fact]
    public void Given_TargetChest_When_Opened_Then_TreasureDrawnAndHunterMarked()
    {
        var random = new FixedRandom(4, 4);
        var state = Game(random);
        state = state.Apply(new Pass(1), random).State;
        state = state.Apply(new RollDice(2), random).State;

        var result = state.Apply(new OpenChest(2, ChestB), random);

        Assert.Contains(Treasure, result.State.Hunter(2).Hand);
        Assert.True(result.State.IsMarked(2));
        Assert.False(result.State.IsMarked(1));
        Assert.Contains(result.Events, e => e is HunterMarked m && m.HunterId == 2);
    }

    [Fact]
    public void Given_ExactlyTwoPa_When_Opened_Then_TurnEndsAutomatically()
    {
        var state = Game(new FixedRandom(2));

        var result = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));

        Assert.Equal(2, result.State.CurrentHunterId);
        Assert.Contains(result.Events, e => e is TurnEnded t && t.HunterId == 1);
    }

    [Fact]
    public void Given_ChestOpened_When_Resolved_Then_OpenerGainsChestXp()
    {
        var state = Game(new FixedRandom(4));

        var result = state.Apply(new OpenChest(1, ChestA), new SeededRandom(1));

        Assert.Equal(5, result.State.Hunter(1).Xp);
    }

    [Fact]
    public void Given_GoldChance_When_RollBelowPercent_Then_GoldInsteadOfCard()
    {
        // FixedRandom.Next(max) devolve max-1: 99 para a chance (falha), então forçamos 100% para testar o ouro.
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(ChestA, false, false) }, Treasure, HunterAt(1, 0, 0), HunterAt(2, 4, 0))
            with { Config = new GameConfig(ChestGoldPercent: 100, ChestGoldMin: 15, ChestGoldMax: 40) };
        state = state.StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new OpenChest(1, ChestA), new FixedRandom());

        var found = Assert.Single(result.Events.OfType<GoldFound>());
        Assert.InRange(found.Amount, 15, 40);
        Assert.Equal(found.Amount, result.State.Hunter(1).Gold);
        Assert.Empty(result.Events.OfType<CardDrawn>());
        Assert.Empty(result.State.Hunter(1).Hand);
        Assert.True(result.State.ChestAt(ChestA)!.IsOpened);
    }

    [Fact]
    public void Given_TargetChest_When_GoldChanceIsFull_Then_StillGivesTreasure()
    {
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(ChestA, false, true) }, Treasure, HunterAt(1, 0, 0), HunterAt(2, 4, 0))
            with { Config = new GameConfig(ChestGoldPercent: 100, BossRound: 0) };
        state = state.StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new OpenChest(1, ChestA), new FixedRandom());

        Assert.Empty(result.Events.OfType<GoldFound>());
        Assert.Contains(Treasure, result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_ManySeeds_When_ChestsOpened_Then_AboutAQuarterGiveGold()
    {
        var gold = 0;
        for (var seed = 1; seed <= 400; seed++)
        {
            var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(ChestA, false, false) }, Treasure, HunterAt(1, 0, 0), HunterAt(2, 4, 0))
                with { Config = new GameConfig() };
            state = state.StartAndRoll(new FixedRandom(4));
            if (state.Apply(new OpenChest(1, ChestA), new SeededRandom(seed)).Events.OfType<GoldFound>().Any())
                gold++;
        }

        Assert.InRange(gold / 400.0, 0.17, 0.33);
    }

    [Fact]
    public void Given_LuckyHunter_When_Opened_Then_LootUsesEffectiveLuck()
    {
        // Mesmo seed: com SOR efetiva maior, a raridade sorteada muda em algum dos casos.
        var baseLuck = HunterStats.Base;
        var lucky = HunterStats.Base with { Luck = 6 };
        var drawsBase = new List<string>();
        var drawsLucky = new List<string>();

        for (var seed = 1; seed <= 30; seed++)
        {
            drawsBase.Add(Open(baseLuck, seed));
            drawsLucky.Add(Open(lucky, seed));
        }

        Assert.NotEqual(drawsBase, drawsLucky);

        static string Open(HunterStats stats, int seed)
        {
            var state = NewGameWithChests(
                    MapBuilder.WithChests5x5(),
                    new[] { new Chest(ChestA, false, false) },
                    Treasure,
                    HunterAt(1, 0, 0, stats), HunterAt(2, 4, 0))
                .StartAndRoll(new FixedRandom(4));
            return state.Apply(new OpenChest(1, ChestA), new SeededRandom(seed)).Events.OfType<CardDrawn>().Single().CardId;
        }
    }
}
