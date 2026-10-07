using BattleHunter.Core.Cards;
using BattleHunter.Core.Progression;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Progression;

public class ShopAndLoadoutTests
{
    private static CardCatalog Cards => TestContent.Default.Cards;
    private static readonly DateTimeOffset Day = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Given_Day_When_Stock_Then_SixCommonsThreeUncommonsDeterministic()
    {
        var a = Shop.StockFor(Day, Cards);
        var b = Shop.StockFor(Day, Cards);
        var other = Shop.StockFor(Day.AddDays(1), Cards);

        Assert.Equal(a, b);
        Assert.Equal(9, a.Count);
        Assert.Equal(6, a.Count(id => Cards.Get(id).Rarity == Rarity.Common));
        Assert.Equal(3, a.Count(id => Cards.Get(id).Rarity == Rarity.Uncommon));
        Assert.Equal(9, a.Distinct().Count());
        Assert.NotEqual(a, other);
        Assert.DoesNotContain(a, id => Cards.Get(id).Type == CardType.Treasure);
    }

    [Fact]
    public void Given_EnoughGold_When_Buy_Then_PriceIsThreeTimesSell()
    {
        var stock = Shop.StockFor(Day, Cards);
        var card = Cards.Get(stock[0]);
        var profile = Profile.New("p", "A") with { Gold = 1000 };

        var after = Shop.Buy(profile, card.Id, Day, Cards)!;

        Assert.Equal(1000 - card.Sell * 3, after.Gold);
        Assert.Contains(card.Id, after.Inventory);
    }

    [Fact]
    public void Given_NotEnoughGoldOrNotInStock_When_Buy_Then_Null()
    {
        var stock = Shop.StockFor(Day, Cards);
        var poor = Profile.New("p", "A") with { Gold = 0 };
        Assert.Null(Shop.Buy(poor, stock[0], Day, Cards));

        var rich = Profile.New("p", "A") with { Gold = 10_000 };
        Assert.Null(Shop.Buy(rich, "greatsword", Day, Cards));
    }

    [Fact]
    public void Given_CardInInventory_When_Sold_Then_GoldBySellValue()
    {
        var profile = Profile.New("p", "A") with { Inventory = new[] { "axe", "axe" }, Loadout = new Loadout(new Equipment(Weapon: "axe"), Array.Empty<string>()) };

        var after = Shop.Sell(profile, "axe", Cards)!;

        Assert.Equal(50, after.Gold);
        Assert.Single(after.Inventory);
        Assert.Null(Shop.Sell(after, "axe", Cards));
    }

    [Fact]
    public void Given_Gold_When_Deposited_Then_MovesToBank()
    {
        var profile = Profile.New("p", "A") with { Gold = 100 };

        var after = Shop.Deposit(profile, 60)!;

        Assert.Equal((40, 60), (after.Gold, after.BankedGold));
        Assert.Null(Shop.Deposit(after, 50));
    }

    [Fact]
    public void Given_LoadoutFromInventory_When_Validated_Then_Ok()
    {
        var profile = Profile.New("p", "A") with { Inventory = new[] { "axe", "leather", "potion", "dagger" } };
        var loadout = new Loadout(new Equipment(Weapon: "axe", Armor: "leather"), new[] { "potion", "dagger" });

        Assert.Null(LoadoutRules.Validate(profile, loadout, Cards));
    }

    [Fact]
    public void Given_LoadoutWithCardNotOwnedOrWrongSlot_When_Validated_Then_Error()
    {
        var profile = Profile.New("p", "A") with { Inventory = new[] { "axe" } };

        Assert.NotNull(LoadoutRules.Validate(profile, new Loadout(Equipment.None, new[] { "potion" }), Cards));
        Assert.NotNull(LoadoutRules.Validate(profile, new Loadout(new Equipment(Armor: "axe"), Array.Empty<string>()), Cards));
        Assert.NotNull(LoadoutRules.Validate(profile with { Inventory = new[] { "axe", "axe" } }, new Loadout(new Equipment(Weapon: "axe"), new[] { "axe", "axe" }), Cards));
        Assert.NotNull(LoadoutRules.Validate(profile with { Inventory = Enumerable.Repeat("potion", 6).ToList() }, new Loadout(Equipment.None, Enumerable.Repeat("potion", 6).ToList()), Cards));
    }

    [Fact]
    public void Given_Profile_When_ToHunterSetup_Then_LevelStatsAndLoadoutCarried()
    {
        var profile = Profile.New("p", "Jef") with { Level = 5, Inventory = new[] { "axe", "potion" }, Loadout = new Loadout(new Equipment(Weapon: "axe"), new[] { "potion" }) };

        var setup = LoadoutRules.ToHunterSetup(profile, 3);

        Assert.Equal((3, "Jef", 5), (setup.Id, setup.Name, setup.Level));
        Assert.Equal(32, setup.Stats.MaxHp);
        Assert.Equal("axe", setup.Equipment!.Weapon);
        Assert.Equal(new[] { "potion" }, setup.Hand);
    }

    [Fact]
    public void Given_MissionsJson_When_Loaded_Then_MatchesGddTable()
    {
        var m = TestContent.Default.Missions;

        Assert.Equal(4, m.All.Count);
        Assert.Equal((12, 30, false, 100, 50, 1), Tuple(m.Get("easy")));
        Assert.Equal((14, 30, true, 250, 120, 5), Tuple(m.Get("normal")));
        Assert.Equal((16, 25, true, 500, 250, 12), Tuple(m.Get("hard")));
        Assert.Equal((14, 30, true, 200, 0, 8), Tuple(m.Get("ranked")));
        Assert.True(m.Get("hard").RewardRareCard);
        Assert.Equal(150, m.Get("hard").BossHpPercent);
        Assert.True(m.Get("ranked").Ranked);

        static (int, int, bool, int, int, int) Tuple(MissionType t) => (t.GridSize, t.MaxRounds, t.Boss, t.RewardGold, t.RewardXp, t.RequiredLevel);
    }
}
