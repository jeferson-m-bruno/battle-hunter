using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class CombatRulesTests
{
    private static readonly Position KoboldCell = new(1, 0);

    /// <summary>Caçador 1 em (0,0) com um Kobold adjacente em (1,0); caçador 2 longe em (4,4)... na saída, sem efeito.</summary>
    private static GameState WithKobold(IRandom random, HunterStats? stats = null, int? koboldHp = null) =>
        NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, stats), HunterAt(2, 4, 3))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0, koboldHp))
            .StartAndRoll(random);

    [Fact]
    public void Given_AdjacentMonster_When_Attacked_Then_DamageFollowsFormulaAndCostsTwoPa()
    {
        // ATQ 4 + d6 3 = 7; DEF 1 + d6 2 = 3; dano 4. Kobold 8 PV -> 4.
        var state = WithKobold(new FixedRandom(5));

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(3, 2));

        var resolved = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.Equal((3, 2, 4, false, false, 4), (resolved.AttackerDie, resolved.DefenderDie, resolved.Damage, resolved.Critical, resolved.Dodged, resolved.TargetHpLeft));
        Assert.Equal(4, result.State.Monster(1).Hp);
        Assert.Equal(3, result.State.ActionPoints);
    }

    [Fact]
    public void Given_WeakAttacker_When_Attacked_Then_MinimumDamageIsOne()
    {
        // ATQ 4 + 1 = 5 contra Orc DEF 3 + 6 = 9 -> max(1, -4) = 1. Orc sem SOR: a rolagem de esquiva (1) não passa.
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 3))
            .WithMonsters(MonsterAt(1, "orc", 1, 0))
            .StartAndRoll(new FixedRandom(5));

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(1, 6, 1));

        var resolved = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.Equal(1, resolved.Damage);
        Assert.False(resolved.Dodged);
        Assert.Equal(17, result.State.Monster(1).Hp);
    }

    [Fact]
    public void Given_AttackerRollsSixAndLuckRoll_When_Attacked_Then_DamageDoubles()
    {
        // ATQ 4 + 6 = 10; DEF 1 + 2 = 3; dano 7; crítico (6 e 1d6 = 1 ≤ SOR 1) -> 14 -> Kobold cai.
        var state = WithKobold(new FixedRandom(5));

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(6, 2, 1));

        var resolved = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.True(resolved.Critical);
        Assert.Equal(14, resolved.Damage);
        Assert.Empty(result.State.Monsters);
    }

    [Fact]
    public void Given_AttackerRollsSixButLuckFails_When_Attacked_Then_NoCritical()
    {
        var state = WithKobold(new FixedRandom(5));

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(6, 2, 5));

        var resolved = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.False(resolved.Critical);
        Assert.Equal(7, resolved.Damage);
    }

    [Fact]
    public void Given_DefenderRollsSixAndLuckRoll_When_Attacked_Then_Dodged()
    {
        // Caçador 2 (SOR 1) é atacado pelo caçador 1: defensor rola 6 e 1d6 = 1 ≤ 1 -> dano zero.
        var state = Rolled(new FixedRandom(5), HunterAt(1, 0, 0), HunterAt(2, 1, 0));

        var result = state.Apply(new Attack(1, new Position(1, 0)), new FixedRandom(3, 6, 1));

        var resolved = Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.True(resolved.Dodged);
        Assert.Equal(0, resolved.Damage);
        Assert.Equal(20, result.State.Hunter(2).Hp);
    }

    [Fact]
    public void Given_TargetNotAdjacent_When_Attacked_Then_Rejected()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 3))
            .WithMonsters(MonsterAt(1, "kobold", 2, 0))
            .StartAndRoll(new FixedRandom(5));

        var result = state.Apply(new Attack(1, new Position(2, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_EmptyCell_When_Attacked_Then_Rejected()
    {
        var state = Rolled(new FixedRandom(5), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var result = state.Apply(new Attack(1, new Position(1, 0)), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_OnePa_When_Attacked_Then_Rejected()
    {
        var state = WithKobold(new FixedRandom(1));

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom());

        Assert.Single(result.Events, e => e is ActionRejected);
    }

    [Fact]
    public void Given_ExactlyTwoPa_When_Attacked_Then_TurnEnds()
    {
        var state = WithKobold(new FixedRandom(2));

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(3, 2));

        Assert.Equal(2, result.State.CurrentHunterId);
        Assert.Contains(result.Events, e => e is TurnEnded t && t.HunterId == 1);
    }

    [Fact]
    public void Given_MonsterDefeated_When_Resolved_Then_XpGainedAndLootRolledAgainstLuck()
    {
        // Kobold com 2 PV cai; XP 10; loot: 1d6 (1) ≤ SOR 1 -> carta sorteada.
        var state = WithKobold(new FixedRandom(5), koboldHp: 2);

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(3, 2, 1));

        Assert.Empty(result.State.Monsters);
        Assert.Equal(10, result.State.Hunter(1).Xp);
        var defeated = Assert.Single(result.Events.OfType<MonsterDefeated>());
        Assert.Equal(("kobold", 1, 10), (defeated.TypeId, defeated.KillerHunterId, defeated.XpGained));
        Assert.Single(result.Events.OfType<CardDrawn>());
        Assert.Single(result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_MonsterDefeated_When_LuckRollFails_Then_NoLoot()
    {
        var state = WithKobold(new FixedRandom(5), koboldHp: 2);

        var result = state.Apply(new Attack(1, KoboldCell), new FixedRandom(3, 2, 6));

        Assert.Empty(result.Events.OfType<CardDrawn>());
        Assert.Empty(result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_HunterReducedToZero_When_Attacked_Then_FallsAndDropsHandAndKillerGainsXp()
    {
        // Caçador 2 com 1 PV e 2 cartas, nível 3. Dano mínimo 1 derruba.
        var victim = HunterAt(2, 1, 0, hand: new[] { "dagger", "boots" }, level: 3) with { Hp = 1 };
        var state = Rolled(new FixedRandom(5), HunterAt(1, 0, 0), victim);

        var result = state.Apply(new Attack(1, new Position(1, 0)), new FixedRandom(1, 5));

        var fallen = result.State.Hunter(2);
        Assert.Equal(HunterStatus.Fallen, fallen.Status);
        Assert.Empty(fallen.Hand);
        Assert.Equal(2, result.State.GroundCards.Count);
        Assert.All(result.State.GroundCards, g => Assert.Equal(new Position(1, 0), g.Position));
        Assert.Equal(30, result.State.Hunter(1).Xp);
        var fell = Assert.Single(result.Events.OfType<HunterFell>());
        Assert.Equal(new[] { "dagger", "boots" }, fell.DroppedCards);
    }

    [Fact]
    public void Given_OnlyOpponentFalls_When_Attacked_Then_GameContinuesForTheAttacker()
    {
        var victim = HunterAt(2, 1, 0) with { Hp = 1 };
        var state = Rolled(new FixedRandom(5), HunterAt(1, 0, 0), victim);

        var result = state.Apply(new Attack(1, new Position(1, 0)), new FixedRandom(1, 5));

        Assert.Equal(GamePhase.Acting, result.State.Phase);
        Assert.Equal(1, result.State.CurrentHunterId);
        Assert.Equal(3, result.State.ActionPoints);
    }
}
