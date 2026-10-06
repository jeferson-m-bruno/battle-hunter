using BattleHunter.Core.Cards;
using BattleHunter.Core.Cards.Effects;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Cards;

public class CardCatalogTests
{
    private static CardCatalog Cards => TestContent.Default.Cards;

    [Fact]
    public void Given_DataCardsJson_When_Loaded_Then_HasGddCountsForSliceTwo()
    {
        Assert.Equal(10, Cards.Where(c => c.Type == CardType.Weapon).Count);
        Assert.Equal(8, Cards.Where(c => c.Type == CardType.Armor).Count);
        Assert.Equal(8, Cards.Where(c => c.Type == CardType.Accessory).Count);
        Assert.Equal(3, Cards.Where(c => c.Type == CardType.Treasure).Count);
    }

    [Fact]
    public void Given_DataCardsJson_When_Validated_Then_EveryEffectIsRegistered()
    {
        var errors = Cards.Validate(EffectRegistry.Default);

        Assert.Empty(errors);
    }

    [Fact]
    public void Given_SwordIron_When_Read_Then_MatchesGddExample()
    {
        var card = Cards.Get("sword_iron");

        Assert.Equal("Espada de Ferro", card.Name);
        Assert.Equal(CardType.Weapon, card.Type);
        Assert.Equal(Rarity.Common, card.Rarity);
        Assert.Equal(20, card.Sell);
        Assert.Equal(4, card.Mods.Atk);
        Assert.Null(card.Effect);
    }

    [Fact]
    public void Given_Axe_When_Read_Then_HasAttackBonusAndSpeedPenalty()
    {
        var card = Cards.Get("axe");

        Assert.Equal(6, card.Mods.Atk);
        Assert.Equal(-1, card.Mods.Spd);
    }

    [Fact]
    public void Given_UnknownId_When_Get_Then_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => Cards.Get("nope"));
    }

    [Fact]
    public void Given_JsonWithUnknownEffect_When_Validated_Then_ReportsError()
    {
        var catalog = CardCatalog.FromJson(
            @"[{ ""id"": ""x"", ""name"": ""X"", ""type"": ""consumable"", ""rarity"": ""common"", ""cost"": 1, ""sell"": 1, ""mods"": null, ""effect"": ""ghost"" }]");

        var errors = catalog.Validate(EffectRegistry.Default);

        Assert.Single(errors);
        Assert.Contains("ghost", errors[0]);
    }

    [Fact]
    public void Given_JsonWithSnakeCaseType_When_Loaded_Then_ParsesSpecialAttack()
    {
        var catalog = CardCatalog.FromJson(
            @"[{ ""id"": ""x"", ""name"": ""X"", ""type"": ""special_attack"", ""rarity"": ""rare"", ""cost"": 2, ""sell"": 1, ""mods"": null, ""effect"": null }]");

        Assert.Equal(CardType.SpecialAttack, catalog.Get("x").Type);
    }
}
