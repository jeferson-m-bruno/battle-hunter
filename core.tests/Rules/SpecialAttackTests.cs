using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

/// <summary>Ataques especiais (2 PA, no lugar do ataque normal, descartam). Caçador base: ATQ 4, DEF 2, SOR 1. Kobold: 8 PV, ATQ 3, DEF 1.</summary>
public class SpecialAttackTests
{
    private static readonly Position Adjacent = new(1, 0);

    private static GameState VersusKobold(string card, int koboldX = 1, int? koboldHp = null, int pa = 4) =>
        NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { card }), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", koboldX, 0, koboldHp))
            .StartAndRoll(new FixedRandom(pa));

    [Fact]
    public void Given_PowerStrike_When_Used_Then_BonusAttackAddsToFormulaAndCostsTwoPa()
    {
        // (4 + 4 + 3) − (1 + 2) = 8 -> Kobold cai.
        var state = VersusKobold("power_strike");

        var result = state.Apply(new UseCard(1, "power_strike", Adjacent), new FixedRandom(3, 2, 6));

        var attack = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.Equal(8, attack.Damage);
        Assert.Empty(result.State.Monsters);
        Assert.Equal(2, result.State.ActionPoints);
        Assert.Empty(result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_PreciseStrike_When_DefenderRollsSix_Then_CannotDodge()
    {
        // Contra o caçador 2 (SOR 1): defensor 6 esquivaria, mas no_dodge. (4+1+3) − (2+6) = 0 -> mínimo 1.
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "precise_strike" }), HunterAt(2, 1, 0));

        var result = state.Apply(new UseCard(1, "precise_strike", Adjacent), new FixedRandom(3, 6, 1, 1));

        var attack = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.False(attack.Dodged);
        Assert.Equal(1, attack.Damage);
        Assert.Equal(19, result.State.Hunter(2).Hp);
    }

    [Fact]
    public void Given_DoubleStrike_When_Used_Then_TwoAttacksOnSameTarget()
    {
        var state = VersusKobold("double_strike");

        var result = state.Apply(new UseCard(1, "double_strike", Adjacent), new FixedRandom(3, 2, 3, 2, 6));

        Assert.Equal(2, result.Events.OfType<AttackResolved>().Count());
        Assert.Empty(result.State.Monsters);
    }

    [Fact]
    public void Given_DoubleStrikeKillsOnFirstHit_When_Used_Then_NoSecondAttack()
    {
        var state = VersusKobold("double_strike", koboldHp: 2);

        var result = state.Apply(new UseCard(1, "double_strike", Adjacent), new FixedRandom(3, 2, 6));

        Assert.Single(result.Events.OfType<AttackResolved>());
    }

    [Fact]
    public void Given_Charge_When_TargetInLineAtRangeThree_Then_MovesAdjacentAndAttacksWithBonus()
    {
        // Kobold em (3,0): avança para (1,0) e (2,0), ataca com +2: (4+2+3) − (1+2) = 6.
        var state = VersusKobold("charge", koboldX: 3);

        var result = state.Apply(new UseCard(1, "charge", new Position(3, 0)), new FixedRandom(3, 2));

        Assert.Equal(new Position(2, 0), result.State.Hunter(1).Position);
        Assert.Equal(2, result.Events.OfType<HunterMoved>().Count());
        Assert.Equal(6, result.Events.OfType<AttackResolved>().Single().Damage);
        Assert.Equal(2, result.State.Monster(1).Hp);
    }

    [Fact]
    public void Given_Charge_When_TargetNotInStraightLine_Then_Rejected()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { "charge" }), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 2, 1))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "charge", new Position(2, 1)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_Charge_When_PathBlocked_Then_Rejected()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { "charge" }), HunterAt(2, 1, 0))
            .WithMonsters(MonsterAt(1, "kobold", 3, 0))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "charge", new Position(3, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_Charge_When_AdjacentTarget_Then_RejectedUseNormalAttack()
    {
        var state = VersusKobold("charge");

        var result = state.Apply(new UseCard(1, "charge", Adjacent), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_Trip_When_HitsHunter_Then_TwoRandomCardsFallToGround()
    {
        // Dano 4 (3 vs 1); roubo falha (1+1 >= 1+1); índices: Next(3) = 2 -> "c", Next(2) = 1 -> "b".
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "trip" }), HunterAt(2, 1, 0, hand: new[] { "a_dagger", "b_boots", "c_tunic" }.Select(Fix).ToList()));

        var result = state.Apply(new UseCard(1, "trip", Adjacent), new FixedRandom(3, 1, 1, 1));

        var dropped = Assert.Single(result.Events.OfType<CardsDropped>());
        Assert.Equal(new[] { "tunic", "boots" }, dropped.Cards);
        Assert.Equal(new[] { "dagger" }, result.State.Hunter(2).Hand);
        Assert.Equal(2, result.State.GroundCards.Count);
        Assert.All(result.State.GroundCards, g => Assert.Equal(Adjacent, g.Position));

        static string Fix(string s) => s.Substring(2);
    }

    [Fact]
    public void Given_Trip_When_Dodged_Then_NoCardsDropped()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, hand: new[] { "trip" }), HunterAt(2, 1, 0, hand: new[] { "dagger" }));

        var result = state.Apply(new UseCard(1, "trip", Adjacent), new FixedRandom(3, 6, 1));

        Assert.Empty(result.Events.OfType<CardsDropped>());
        Assert.Single(result.State.Hunter(2).Hand);
    }

    [Fact]
    public void Given_Whirlwind_When_TwoMonstersAdjacent_Then_BothAttacked()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { "whirlwind" }), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0), MonsterAt(2, "kobold", 0, 1))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "whirlwind"), new FixedRandom(3, 2, 3, 2));

        Assert.Equal(2, result.Events.OfType<AttackResolved>().Count());
        Assert.All(result.State.Monsters, m => Assert.Equal(4, m.Hp));
    }

    [Fact]
    public void Given_Whirlwind_When_NobodyAdjacent_Then_Rejected()
    {
        var state = VersusKobold("whirlwind", koboldX: 3);

        var result = state.Apply(new UseCard(1, "whirlwind"), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_LifeSteal_When_Hits_Then_HealsHalfTheDamage()
    {
        // (4+5) − (1+1) = 7 de dano -> cura 3.
        var hunter = HunterAt(1, 0, 0, hand: new[] { "life_steal" }) with { Hp = 10 };
        var state = NewGame(MapBuilder.Open5x5(), hunter, HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0))
            .StartAndRoll(new FixedRandom(4));

        var result = state.Apply(new UseCard(1, "life_steal", Adjacent), new FixedRandom(5, 1));

        Assert.Equal(1, result.State.Monster(1).Hp);
        Assert.Equal(13, result.State.Hunter(1).Hp);
        Assert.Contains(result.Events, e => e is HunterHealed h && h.Amount == 3);
    }

    [Fact]
    public void Given_OnePa_When_SpecialAttackUsed_Then_Rejected()
    {
        var state = VersusKobold("power_strike", pa: 1);

        var result = state.Apply(new UseCard(1, "power_strike", Adjacent), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }
}
