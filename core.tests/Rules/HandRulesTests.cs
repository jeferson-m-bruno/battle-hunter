using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class HandRulesTests
{
    [Fact]
    public void Given_AxeInHand_When_Equipped_Then_CostsOnePaAndStatsChange()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "axe" }), HunterAt(2, 4, 0));

        var result = state.Apply(new Equip(1, "axe"), new FixedRandom());

        var hunter = result.State.Hunter(1);
        Assert.Equal(3, result.State.ActionPoints);
        Assert.Equal("axe", hunter.Equipment.Weapon);
        Assert.Empty(hunter.Hand);
        var effective = StatRules.Effective(hunter, TestContent.Default);
        Assert.Equal(10, effective.Attack);
        Assert.Equal(-1, effective.Speed);
        var equipped = Assert.Single(result.Events.OfType<CardEquipped>());
        Assert.Equal((EquipmentSlot.Weapon, (string?)null), (equipped.Slot, equipped.UnequippedCardId));
    }

    [Fact]
    public void Given_WeaponEquipped_When_AnotherWeaponEquipped_Then_PreviousReturnsToHand()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "sword_iron" }, equipment: new Equipment(Weapon: "dagger")), HunterAt(2, 4, 0));

        var result = state.Apply(new Equip(1, "sword_iron"), new FixedRandom());

        var hunter = result.State.Hunter(1);
        Assert.Equal("sword_iron", hunter.Equipment.Weapon);
        Assert.Equal(new[] { "dagger" }, hunter.Hand);
    }

    [Fact]
    public void Given_RingOfLuck_When_Equipped_Then_LuckIsBasePlusTwo()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "ring_luck" }), HunterAt(2, 4, 0));

        var result = state.Apply(new Equip(1, "ring_luck"), new FixedRandom());

        Assert.Equal(3, StatRules.Effective(result.State.Hunter(1), TestContent.Default).Luck);
        Assert.Equal("ring_luck", result.State.Hunter(1).Equipment.Accessory);
    }

    [Fact]
    public void Given_BootsEquipped_When_DiceRolled_Then_SpeedBonusAddsToPa()
    {
        var (state, _) = Started(new FixedRandom(), HunterAt(1, 0, 0, equipment: new Equipment(Accessory: "boots")), HunterAt(2, 4, 0));

        var result = state.Apply(new RollDice(1), new FixedRandom(3));

        Assert.Equal(4, result.State.ActionPoints);
        Assert.Equal(1, result.Events.OfType<DiceRolled>().Single().SpeedBonus);
    }

    [Fact]
    public void Given_CardNotInHand_When_Equipped_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new Equip(1, "axe"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_TreasureInHand_When_Equipped_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "treasure_lost_crown" }), HunterAt(2, 4, 0));

        var result = state.Apply(new Equip(1, "treasure_lost_crown"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_NoPa_When_Equipped_Then_Rejected()
    {
        // 1 PA: mover gasta tudo e o turno passa; equipar fora do turno é rejeitado.
        var state = Rolled(new FixedRandom(1), HunterAt(1, 0, 0, hand: new[] { "axe" }), HunterAt(2, 4, 0));
        state = state.Apply(new MoveTo(1, new Position(1, 0)), new FixedRandom()).State;

        var result = state.Apply(new Equip(1, "axe"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_CardInHand_When_Discarded_Then_RemovedWithoutCost()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "dagger", "boots" }), HunterAt(2, 4, 0));

        var result = state.Apply(new Discard(1, "dagger"), new FixedRandom());

        Assert.Equal(new[] { "boots" }, result.State.Hunter(1).Hand);
        Assert.Equal(4, result.State.ActionPoints);
        Assert.Contains(result.Events, e => e is CardDiscarded d && d.CardId == "dagger");
    }

    [Fact]
    public void Given_TargetTreasureInHand_When_Discarded_Then_Rejected()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), "treasure_lost_crown",
                HunterAt(1, 0, 0, hand: new[] { "treasure_lost_crown" }), HunterAt(2, 4, 0))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new Discard(1, "treasure_lost_crown"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
        Assert.True(result.State.IsMarked(1));
    }
}
