using BattleHunter.Core.Cards;
using BattleHunter.Core.Rules;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Cards;

public class LootRollerTests
{
    private static GameContent Content => TestContent.Default;
    private static LootTable Chest => Content.LootTable(GameContent.ChestLootTableId);

    [Fact]
    public void Given_ChestTable_When_Loaded_Then_HasGddWeights()
    {
        var rare = Chest.Rarities.Single(r => r.Rarity == Rarity.Rare);

        Assert.Equal(10, rare.Weight);
        Assert.Equal(3, rare.PerLuck);
        Assert.Equal(13, rare.EffectiveWeight(1));
        Assert.Equal(28, rare.EffectiveWeight(6));
    }

    [Fact]
    public void Given_HigherLuck_When_RollingManyTimes_Then_MoreRares()
    {
        var lowLuck = RareShare(luck: 1, seed: 11);
        var highLuck = RareShare(luck: 6, seed: 11);

        Assert.InRange(lowLuck, 0.08, 0.18);
        Assert.InRange(highLuck, 0.16, 0.28);
        Assert.True(highLuck > lowLuck);
    }

    [Fact]
    public void Given_ChestTable_When_Rolled_Then_NeverReturnsTreasure()
    {
        var random = new SeededRandom(3);

        for (var i = 0; i < 500; i++)
            Assert.NotEqual(CardType.Treasure, LootRoller.Roll(Chest, Content.Cards, 3, random).Type);
    }

    [Fact]
    public void Given_SameSeed_When_Rolled_Then_SameCard()
    {
        var a = LootRoller.Roll(Chest, Content.Cards, 2, new SeededRandom(9));
        var b = LootRoller.Roll(Chest, Content.Cards, 2, new SeededRandom(9));

        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void Given_RarityWithoutCards_When_Rolled_Then_FallsBackToLowerRarity()
    {
        var catalog = new CardCatalog(new[]
        {
            new Card("c1", "C1", CardType.Weapon, Rarity.Common, 1, 1, StatMods.None, null),
        });
        var table = new LootTable("t", new[] { CardType.Weapon }, new[] { new RarityWeight(Rarity.Rare, 100, 0) });

        var card = LootRoller.Roll(table, catalog, 0, new SeededRandom(1));

        Assert.Equal("c1", card.Id);
    }

    private static double RareShare(int luck, int seed)
    {
        var random = new SeededRandom(seed);
        var rares = Enumerable.Range(0, 4000).Count(_ => LootRoller.RollRarity(Chest, luck, random) == Rarity.Rare);
        return rares / 4000.0;
    }
}
