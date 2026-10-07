using BattleHunter.Core.Progression;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Progression;

public class MatchRewardsTests
{
    private static Profile Base() => Profile.New("p", "Jef", season: "2026-10") with
    {
        Gold = 100,
        Inventory = new[] { "dagger", "potion", "boots", "axe" },
        Loadout = new Loadout(new Equipment(Weapon: "axe"), new[] { "dagger", "potion" }),
    };

    private static (Profile, RewardSummary) Apply(Hunter final, bool won, string mission = "easy", Profile? profile = null) =>
        MatchRewards.Apply(profile ?? Base(), final, won, TestContent.Default.Missions.Get(mission), TestContent.Default, new SeededRandom(1), "2026-10");

    [Fact]
    public void Given_Win_When_Applied_Then_MissionGoldAndXpAndCardsKept()
    {
        var final = HunterAt(1, 0, 0, hand: new[] { "dagger", "sword_iron" }, equipment: new Equipment(Weapon: "axe")) with { Status = HunterStatus.Exited, Xp = 30, Gold = 20 };

        var (p, r) = Apply(final, won: true);

        Assert.True(r.Won);
        Assert.Equal(80, r.XpGained);
        Assert.Equal(100 + 20 + 100, p.Gold);
        Assert.Equal(0, r.GoldLost);
        Assert.Equal(new[] { "dagger", "sword_iron" }, r.CardsKept);
        Assert.Empty(r.CardsSold);
        // Inventário: tirou dagger, potion (levados) e axe (equipado); voltou dagger, sword_iron e axe; boots ficou.
        Assert.Equal(new[] { "axe", "boots", "dagger", "sword_iron" }, p.Inventory.OrderBy(x => x));
        Assert.Equal(new[] { "dagger", "sword_iron" }, p.Loadout.Hand);
        Assert.Equal("axe", p.Loadout.Equipment.Weapon);
    }

    [Fact]
    public void Given_SevenCardsInHand_When_Applied_Then_FiveKeptAndTwoSold()
    {
        var hand = new[] { "dagger", "potion", "boots", "bread", "rock", "sword_iron", "tunic" };
        var final = HunterAt(1, 0, 0, hand: hand) with { Status = HunterStatus.Exited };

        var (p, r) = Apply(final, won: false);

        Assert.Equal(5, r.CardsKept.Count);
        Assert.Equal(new[] { "sword_iron", "tunic" }, r.CardsSold);
        Assert.Equal(20 + 10, r.GoldFromSales);
        Assert.Equal(100 + 30, p.Gold);
        Assert.Equal(0, r.GoldFromMission);
    }

    [Fact]
    public void Given_Fallen_When_Applied_Then_TenPercentOfUndepositedGoldLost()
    {
        var profile = Base() with { Gold = 200, BankedGold = 500 };
        var final = HunterAt(1, 0, 0) with { Status = HunterStatus.Fallen, Gold = 0 };

        var (p, r) = Apply(final, won: false, profile: profile);

        Assert.Equal(20, r.GoldLost);
        Assert.Equal(180, p.Gold);
        Assert.Equal(500, p.BankedGold);
        Assert.Empty(p.Loadout.Hand);
    }

    [Fact]
    public void Given_HardMissionWon_When_Applied_Then_RareCardAdded()
    {
        var final = HunterAt(1, 0, 0) with { Status = HunterStatus.Exited };

        var (p, r) = Apply(final, won: true, mission: "hard");

        Assert.NotNull(r.RareCard);
        Assert.Equal(Core.Cards.Rarity.Rare, TestContent.Default.Cards.Get(r.RareCard!).Rarity);
        Assert.Contains(r.RareCard, p.Inventory);
        Assert.Equal(250, r.XpGained);
    }

    [Fact]
    public void Given_XpEnoughToLevel_When_Applied_Then_LevelAndPointGranted()
    {
        var final = HunterAt(1, 0, 0) with { Status = HunterStatus.Exited, Xp = 60 };

        var (p, r) = Apply(final, won: true);

        Assert.Equal(110, p.Xp);
        Assert.Equal(2, p.Level);
        Assert.Equal(1, r.LevelsGained);
        Assert.Equal(1, p.UnspentPoints);
    }

    [Fact]
    public void Given_RankedMission_When_Applied_Then_RankPointsFollowOutcome()
    {
        var won = Apply(HunterAt(1, 0, 0) with { Status = HunterStatus.Exited }, won: true, mission: "ranked");
        Assert.Equal(20, won.Item2.RankDelta);
        Assert.Equal(20, won.Item1.RankPoints);

        var fell = Apply(HunterAt(1, 0, 0) with { Status = HunterStatus.Fallen }, won: false, mission: "ranked", profile: Base() with { RankPoints = 5 });
        Assert.Equal(-5, fell.Item2.RankDelta);
        Assert.Equal(0, fell.Item1.RankPoints);

        var casual = Apply(HunterAt(1, 0, 0) with { Status = HunterStatus.Exited }, won: true, mission: "easy");
        Assert.Equal(0, casual.Item2.RankDelta);
    }

    [Fact]
    public void Given_NewSeason_When_Applied_Then_RankResetsBeforeCounting()
    {
        var profile = Base() with { RankPoints = 300, Season = "2026-09" };

        var (p, _) = Apply(HunterAt(1, 0, 0) with { Status = HunterStatus.Exited }, won: false, mission: "ranked", profile: profile);

        Assert.Equal("2026-10", p.Season);
        Assert.Equal(5, p.RankPoints);
    }
}
