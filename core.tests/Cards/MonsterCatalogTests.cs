using BattleHunter.Core.Cards;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Cards;

public class MonsterCatalogTests
{
    private static MonsterCatalog Monsters => TestContent.Default.Monsters;

    [Theory]
    [InlineData("kobold", 8, 3, 1, 10, "monster_common")]
    [InlineData("skeleton", 12, 4, 2, 15, "monster_common")]
    [InlineData("spider", 10, 3, 1, 15, "monster_common")]
    [InlineData("orc", 18, 6, 3, 25, "monster_uncommon")]
    [InlineData("mimic", 15, 5, 4, 30, "monster_rare")]
    [InlineData("dragon", 60, 10, 6, 150, "boss")]
    public void Given_MonstersJson_When_Loaded_Then_MatchesGddTable(string id, int hp, int atk, int def, int xp, string loot)
    {
        var m = Monsters.Get(id);

        Assert.Equal((hp, atk, def, xp, loot), (m.Hp, m.Atk, m.Def, m.Xp, m.Loot));
        Assert.True(TestContent.Default.LootTables.ContainsKey(m.Loot));
    }

    [Fact]
    public void Given_MonstersJson_When_Loaded_Then_OnlyFourTypesSpawn()
    {
        Assert.Equal(new[] { "kobold", "orc", "skeleton", "spider" }, Monsters.Spawnable.Select(m => m.Id));
        Assert.True(Monsters.Get("spider").Poison);
        Assert.Equal(MonsterBehavior.Boss, Monsters.Get("dragon").Behavior);
        Assert.Equal(MonsterBehavior.Mimic, Monsters.Get("mimic").Behavior);
    }
}
