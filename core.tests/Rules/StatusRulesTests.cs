using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class StatusRulesTests
{
    [Fact]
    public void Given_PoisonedHunter_When_TurnStarts_Then_LosesTwoHpAndCounterDrops()
    {
        var random = new FixedRandom(1);
        var poisoned = HunterAt(2, 4, 0).WithStatus(StatusKind.Poisoned, 3);
        var state = Rolled(random, HunterAt(1, 0, 0), poisoned);

        var result = state.Apply(new Pass(1), random);

        Assert.Equal(18, result.State.Hunter(2).Hp);
        Assert.Equal(2, result.State.Hunter(2).Statuses.Single().TurnsLeft);
        Assert.Contains(result.Events, e => e is DamageDealt d && d.Cause == "poison" && d.Source.Kind == CombatantKind.Environment);
        Assert.Equal(2, result.State.CurrentHunterId);
    }

    [Fact]
    public void Given_PoisonedHunter_When_ThreeTurnsPass_Then_PoisonExpires()
    {
        var random = new FixedRandom();
        var poisoned = HunterAt(2, 4, 0).WithStatus(StatusKind.Poisoned, 3);
        var state = Rolled(random, HunterAt(1, 0, 0), poisoned);

        ReducerResult result = new(state, Array.Empty<GameEvent>());
        for (var i = 0; i < 3; i++)
        {
            result = result.State.Apply(new Pass(1), random);          // tique no início do turno do 2
            result = result.State.Apply(new RollDice(2), random);
            result = result.State.Apply(new Pass(2), random);
            result = result.State.Apply(new RollDice(1), random);
        }

        Assert.Equal(14, result.State.Hunter(2).Hp);
        Assert.Empty(result.State.Hunter(2).Statuses);
    }

    [Fact]
    public void Given_PoisonedHunterWithTwoHp_When_TurnStarts_Then_FallsAndTurnPasses()
    {
        var random = new FixedRandom(1);
        var poisoned = HunterAt(2, 4, 0, hand: new[] { "dagger" }).WithStatus(StatusKind.Poisoned, 3) with { Hp = 2 };
        var state = Rolled(random, HunterAt(1, 0, 0), poisoned);

        var result = state.Apply(new Pass(1), random);

        Assert.Equal(HunterStatus.Fallen, result.State.Hunter(2).Status);
        Assert.Contains(result.Events, e => e is HunterFell f && f.KilledBy.Kind == CombatantKind.Environment);
        Assert.Single(result.State.GroundCards);
        Assert.Equal(1, result.State.CurrentHunterId);
        Assert.Equal(2, result.State.Round);
    }

    [Fact]
    public void Given_SpiderHits_When_MonsterPhase_Then_HunterIsPoisoned()
    {
        // Aranha ATQ 3 + 4 = 7 vs DEF 2 + 1 = 3 -> 4 de dano e Veneno; o primeiro tique (−2) vem no turno seguinte, no mesmo Pass.
        var random = new FixedRandom(1, 4, 1);
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0))
            .WithMonsters(MonsterAt(1, "spider", 1, 0))
            .StartAndRoll(random);

        var result = state.Apply(new Pass(1), random);

        Assert.Equal(14, result.State.Hunter(1).Hp);
        Assert.Equal(2, result.State.Hunter(1).Statuses.Single().TurnsLeft);
        Assert.Contains(result.Events, e => e is StatusApplied s && s.Kind == StatusKind.Poisoned && s.Turns == 3);
    }

    [Fact]
    public void Given_SpiderDodged_When_MonsterPhase_Then_NoPoison()
    {
        var random = new FixedRandom(1, 4, 6, 1);
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0))
            .WithMonsters(MonsterAt(1, "spider", 1, 0))
            .StartAndRoll(random);

        var result = state.Apply(new Pass(1), random);

        Assert.True(result.Events.OfType<AttackResolved>().Single().Dodged);
        Assert.False(result.State.Hunter(1).HasStatus(StatusKind.Poisoned));
    }

    [Fact]
    public void Given_HeavyArmor_When_Equipped_Then_SlowComesFromModifiers()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0, equipment: new Equipment(Armor: "plate")), HunterAt(2, 4, 0));

        Assert.Equal(-1, StatRules.Effective(state.Hunter(1), TestContent.Default).Speed);
    }
}
