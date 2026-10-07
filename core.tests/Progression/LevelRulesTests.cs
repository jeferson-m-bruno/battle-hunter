using BattleHunter.Core.Cards;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Progression;

public class LevelRulesTests
{
    private static LevelTable Levels => TestContent.Default.Levels;

    [Fact]
    public void Given_DataLevelsJson_When_Loaded_Then_ThirtyLevelsStrictlyIncreasingFromZero()
    {
        Assert.Equal(30, Levels.Count);
        Assert.Equal(0, Levels.XpFor(1));
        Assert.Equal(100, Levels.XpFor(2));
        for (var n = 2; n <= 30; n++)
            Assert.True(Levels.XpFor(n) > Levels.XpFor(n - 1));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(700, 5)]
    [InlineData(23_200, 30)]
    [InlineData(999_999, 30)]
    public void Given_Xp_When_LevelFor_Then_MatchesTable(int xp, int level)
    {
        Assert.Equal(level, Levels.LevelFor(xp));
    }

    [Fact]
    public void Given_Level1_When_StatsFor_Then_Base()
    {
        var p = Profile.New("p", "A");

        Assert.Equal(Core.State.HunterStats.Base, LevelRules.StatsFor(p));
    }

    [Fact]
    public void Given_Level5_When_StatsFor_Then_AutomaticGainsApplied()
    {
        // n = 4: +12 PV, +4 ATQ, +2 DEF, +1 VEL, +2 SOR.
        var p = Profile.New("p", "A") with { Level = 5 };

        var s = LevelRules.StatsFor(p);

        Assert.Equal((32, 8, 4, 1, 3), (s.MaxHp, s.Attack, s.Defense, s.Speed, s.Luck));
    }

    [Fact]
    public void Given_FreePoints_When_StatsFor_Then_Added()
    {
        var p = Profile.New("p", "A") with { Points = new StatPoints(Hp: 2, Luck: 1) };

        var s = LevelRules.StatsFor(p);

        Assert.Equal(22, s.MaxHp);
        Assert.Equal(2, s.Luck);
    }

    [Fact]
    public void Given_XpCrossingTwoLevels_When_Granted_Then_LevelsAndUnspentPointsFollow()
    {
        var p = Profile.New("p", "A");

        var (after, gained) = LevelRules.GrantXp(p, 260, Levels);

        Assert.Equal(3, after.Level);
        Assert.Equal(2, gained);
        Assert.Equal(2, after.UnspentPoints);
        Assert.Equal(260, after.Xp);
    }

    [Fact]
    public void Given_UnspentPoint_When_Allocated_Then_StatRisesAndPointConsumed()
    {
        var p = Profile.New("p", "A") with { UnspentPoints = 1 };

        var after = LevelRules.AllocatePoint(p, LevelRules.Stat.Atk)!;

        Assert.Equal(1, after.Points.Atk);
        Assert.Equal(0, after.UnspentPoints);
        Assert.Null(LevelRules.AllocatePoint(after, LevelRules.Stat.Def));
    }

    [Fact]
    public void Given_Level30_When_MoreXp_Then_StaysAt30()
    {
        var p = Profile.New("p", "A") with { Level = 30, Xp = 23_200 };

        var (after, gained) = LevelRules.GrantXp(p, 5000, Levels);

        Assert.Equal(30, after.Level);
        Assert.Equal(0, gained);
        Assert.Equal(0, LevelRules.XpToNextLevel(after, Levels));
    }
}
