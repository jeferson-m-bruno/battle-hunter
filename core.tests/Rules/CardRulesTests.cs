using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

/// <summary>Consumíveis via UseCard. Caçador 1 em (0,0), caçador 2 em (4,3); dado do turno = 4 PA salvo indicação.</summary>
public class CardRulesTests
{
    private static GameState WithHand(int hp, params string[] hand) =>
        Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: hand) with { Hp = hp }, HunterAt(2, 4, 3));

    [Fact]
    public void Given_Potion_When_Used_Then_HealsEightCostsOnePaAndDiscards()
    {
        var state = WithHand(5, "potion");

        var result = state.Apply(new UseCard(1, "potion"), new FixedRandom());

        Assert.Equal(13, result.State.Hunter(1).Hp);
        Assert.Equal(3, result.State.ActionPoints);
        Assert.Empty(result.State.Hunter(1).Hand);
        Assert.Contains(result.Events, e => e is CardUsed u && u.CardId == "potion" && u.ActionPointsLeft == 3);
        Assert.Contains(result.Events, e => e is HunterHealed h && h.Amount == 8 && h.HpLeft == 13);
    }

    [Fact]
    public void Given_NearlyFullHp_When_Healed_Then_CappedAtEffectiveMax()
    {
        var state = WithHand(18, "potion");

        var result = state.Apply(new UseCard(1, "potion"), new FixedRandom());

        Assert.Equal(20, result.State.Hunter(1).Hp);
    }

    [Fact]
    public void Given_AmuletOfLife_When_Healed_Then_MaxIncludesEquipmentBonus()
    {
        var hunter = HunterAt(1, 0, 0, hand: new[] { "elixir" }, equipment: new Equipment(Accessory: "amulet_life")) with { Hp = 3 };
        var state = Rolled(new FixedRandom(4), hunter, HunterAt(2, 4, 3));

        var result = state.Apply(new UseCard(1, "elixir"), new FixedRandom());

        Assert.Equal(25, result.State.Hunter(1).Hp);
    }

    [Fact]
    public void Given_Poisoned_When_AntidoteUsed_Then_StatusRemoved()
    {
        var poisoned = HunterAt(1, 0, 0, hand: new[] { "antidote" }).WithStatus(StatusKind.Poisoned, 3);
        var state = Rolled(new FixedRandom(4), poisoned, HunterAt(2, 4, 3));

        var result = state.Apply(new UseCard(1, "antidote"), new FixedRandom());

        Assert.False(result.State.Hunter(1).HasStatus(StatusKind.Poisoned));
        Assert.Contains(result.Events, e => e is StatusRemoved r && r.Kind == StatusKind.Poisoned);
    }

    [Fact]
    public void Given_Adrenaline_When_Used_Then_GainsTwoPaNetOfCost()
    {
        var state = WithHand(20, "adrenaline");

        var result = state.Apply(new UseCard(1, "adrenaline"), new FixedRandom());

        Assert.Equal(5, result.State.ActionPoints);
        Assert.Contains(result.Events, e => e is ActionPointsGained g && g.Amount == 2);
    }

    [Fact]
    public void Given_Bomb_When_ThrownAtCell_Then_ThreeByThreeAreaTakesFixedDamage()
    {
        // Caçador 1 em (0,2) joga em (2,2): área x 1..3, y 1..3 pega o Kobold (2,1) e o caçador 2 (3,3), não o atirador.
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 2, hand: new[] { "bomb" }), HunterAt(2, 3, 3))
            .WithMonsters(MonsterAt(1, "kobold", 2, 1))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "bomb", new Position(2, 2)), new FixedRandom());

        Assert.Equal(5, result.State.Monster(1).Hp);
        Assert.Equal(17, result.State.Hunter(2).Hp);
        Assert.Equal(20, result.State.Hunter(1).Hp);
        Assert.Equal(2, result.Events.OfType<DamageDealt>().Count());
        Assert.All(result.Events.OfType<DamageDealt>(), d => Assert.Equal("bomb", d.Cause));
    }

    [Fact]
    public void Given_BombTargetTooFar_When_Used_Then_RejectedAndCardKept()
    {
        var state = WithHand(20, "bomb");

        var result = state.Apply(new UseCard(1, "bomb", new Position(4, 4)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.Single(result.State.Hunter(1).Hand);
        Assert.Equal(4, result.State.ActionPoints);
    }

    [Fact]
    public void Given_BombKillsMonster_When_Used_Then_ThrowerGetsXp()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 2, hand: new[] { "bomb_large" }), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 2, 1, hp: 3))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "bomb_large", new Position(2, 2)), new FixedRandom(6));

        Assert.Empty(result.State.Monsters);
        Assert.Equal(10, result.State.Hunter(1).Xp);
    }

    [Fact]
    public void Given_Rock_When_ThrownAtMonsterInRange_Then_FixedDamageNoDice()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { "rock" }), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 3, 0))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "rock", new Position(3, 0)), new FixedRandom());

        Assert.Equal(6, result.State.Monster(1).Hp);
        Assert.Empty(result.Events.OfType<AttackResolved>());
    }

    [Fact]
    public void Given_EquipmentCard_When_UsedAsConsumable_Then_Rejected()
    {
        var state = WithHand(20, "axe");

        var result = state.Apply(new UseCard(1, "axe"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_CardNotInHand_When_Used_Then_Rejected()
    {
        var state = WithHand(20);

        var result = state.Apply(new UseCard(1, "potion"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_LastPa_When_ConsumableUsed_Then_TurnEnds()
    {
        var state = Rolled(new FixedRandom(1), HunterAt(1, 0, 0, hand: new[] { "bread" }), HunterAt(2, 4, 3));

        var result = state.Apply(new UseCard(1, "bread"), new FixedRandom());

        Assert.Equal(2, result.State.CurrentHunterId);
    }
}
