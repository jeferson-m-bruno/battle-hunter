using BattleHunter.Core.Ai;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Ai;

public class HunterBrainTests
{
    private const string Treasure = "treasure_dragon_eye";

    private static GameAction Decide(GameState state, int hunterId, AiProfile profile = AiProfile.Balanced, AiMemory? memory = null) =>
        HunterBrain.Decide(state, hunterId, profile, memory ?? new AiMemory(), TestContent.Default);

    [Fact]
    public void Given_AwaitingRoll_When_Decided_Then_RollsDice()
    {
        var (state, _) = Started(new FixedRandom());

        Assert.IsType<RollDice>(Decide(state, state.CurrentHunterId));
    }

    [Fact]
    public void Given_TreasureInHand_When_Decided_Then_StepsTowardExit()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0, hand: new[] { Treasure }), HunterAt(2, 4, 0))
            .StartAndRoll(new FixedRandom(4));

        var action = Assert.IsType<MoveTo>(Decide(state, 1));

        Assert.Equal(1, action.Target.DistanceTo(new Position(0, 0)));
        Assert.True(action.Target.DistanceTo(new Position(4, 4)) < 8);
    }

    [Fact]
    public void Given_TreasureInHandOnExit_When_Decided_Then_Exits()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 4, 4, hand: new[] { Treasure }), HunterAt(2, 0, 0))
            .StartAndRoll(new FixedRandom(4));

        Assert.IsType<Exit>(Decide(state, 1));
    }

    [Fact]
    public void Given_MarkedHunterAdjacent_When_Decided_Then_Attacks()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 1, 0, hand: new[] { Treasure }))
            .StartAndRoll(new FixedRandom(4));

        var attack = Assert.IsType<Attack>(Decide(state, 1));
        Assert.Equal(new Position(1, 0), attack.Target);
    }

    [Fact]
    public void Given_MarkedHunterTwoStepsAway_When_Decided_Then_MovesCloser()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 3, 0, hand: new[] { Treasure }))
            .StartAndRoll(new FixedRandom(4));

        var move = Assert.IsType<MoveTo>(Decide(state, 1));
        Assert.Equal(new Position(1, 0), move.Target);
    }

    [Fact]
    public void Given_MarkedHunterFarAway_When_Decided_Then_DoesNotChase()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 4, hand: new[] { Treasure }))
            .StartAndRoll(new FixedRandom(4));

        var action = Decide(state, 1);

        // Sem baú nem tesouro ao alcance: explora (move) em vez de perseguir a 8 passos.
        Assert.IsType<MoveTo>(action);
    }

    [Fact]
    public void Given_LowHpAndPotion_When_Decided_Then_HealsBeforeAnything()
    {
        var hunter = HunterAt(1, 0, 0, hand: new[] { "potion" }) with { Hp = 4 };
        var state = NewGame(MapBuilder.WithChests5x5(), hunter, HunterAt(2, 4, 3)).StartAndRoll(new FixedRandom(4));

        var use = Assert.IsType<UseCard>(Decide(state, 1));
        Assert.Equal("potion", use.CardId);
    }

    [Fact]
    public void Given_LowHpAndMonsterAdjacent_When_Decided_Then_FleesAwayFromIt()
    {
        var hunter = HunterAt(1, 1, 1) with { Hp = 6 };
        var state = NewGame(MapBuilder.Open5x5(), hunter, HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "orc", 2, 1))
            .StartAndRoll(new FixedRandom(4));

        var move = Assert.IsType<MoveTo>(Decide(state, 1));
        Assert.Equal(2, move.Target.DistanceTo(new Position(2, 1)));
        Assert.True(move.Target.IsOrthogonallyAdjacentTo(new Position(1, 1)));
    }

    [Fact]
    public void Given_HealthyAndMonsterAdjacent_When_Decided_Then_Attacks()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0))
            .StartAndRoll(new FixedRandom(4));

        var attack = Assert.IsType<Attack>(Decide(state, 1));
        Assert.Equal(new Position(1, 0), attack.Target);
    }

    [Fact]
    public void Given_SpecialAttackInHand_When_AttackingAdjacentMonster_Then_UsesTheCard()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { "power_strike" }), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0))
            .StartAndRoll(new FixedRandom(4));

        var use = Assert.IsType<UseCard>(Decide(state, 1));
        Assert.Equal(("power_strike", new Position(1, 0)), (use.CardId, use.Target));
    }

    [Fact]
    public void Given_ChestAdjacent_When_Decided_Then_OpensIt()
    {
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(new Position(1, 0), false, false) }, Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 3))
            .StartAndRoll(new FixedRandom(4));

        var open = Assert.IsType<OpenChest>(Decide(state, 1));
        Assert.Equal(new Position(1, 0), open.ChestPosition);
    }

    [Fact]
    public void Given_ChestAdjacentAndFullHand_When_Decided_Then_DiscardsCheapestFirst()
    {
        var hand = Enumerable.Repeat("axe", 9).Append("bread").ToList();
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(new Position(1, 0), false, false) }, Treasure,
                HunterAt(1, 0, 0, hand: hand), HunterAt(2, 4, 3))
            .StartAndRoll(new FixedRandom(4));

        var discard = Assert.IsType<Discard>(Decide(state, 1));
        Assert.Equal("bread", discard.CardId);
    }

    [Fact]
    public void Given_ChestThreeStepsAway_When_Decided_Then_MovesTowardIt()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), new[] { new Chest(new Position(3, 0), false, false) }, Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 4))
            .StartAndRoll(new FixedRandom(4));

        var move = Assert.IsType<MoveTo>(Decide(state, 1));
        Assert.Equal(new Position(1, 0), move.Target);
    }

    [Fact]
    public void Given_ChestAndMonsterBothAdjacent_When_Decided_Then_ProfileDecides()
    {
        // Baú em (1,0) e Kobold em (0,1), ambos adjacentes. Base: baú 50 > atacar 40. Agressivo: atacar 70 > baú 50. Ganancioso: baú 80.
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(new Position(1, 0), false, false) }, Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 0, 1))
            .StartAndRoll(new FixedRandom(4));

        Assert.IsType<OpenChest>(Decide(state, 1, AiProfile.Balanced));
        Assert.IsType<OpenChest>(Decide(state, 1, AiProfile.Greedy));
        Assert.IsType<Attack>(Decide(state, 1, AiProfile.Aggressive));
    }

    [Fact]
    public void Given_BetterWeaponInHand_When_NothingElseToDo_Then_Equips()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 2, 2, hand: new[] { "sword_iron" }), HunterAt(2, 4, 0));
        var memory = new AiMemory();
        foreach (var y in Enumerable.Range(0, 5))
            foreach (var x in Enumerable.Range(0, 5))
                memory.Observe(HunterView.For(state with { Hunters = new[] { HunterAt(1, x, y), HunterAt(2, 4, 0) } }, 1, TestContent.Default));

        var equip = Assert.IsType<Equip>(Decide(state, 1, memory: memory));
        Assert.Equal("sword_iron", equip.CardId);
    }

    [Fact]
    public void Given_NothingAround_When_Decided_Then_ExploresNearestUnseenCell()
    {
        var state = Rolled(new FixedRandom(4), HunterAt(1, 0, 0), HunterAt(2, 4, 0));

        var move = Assert.IsType<MoveTo>(Decide(state, 1));
        Assert.Equal(1, move.Target.DistanceTo(new Position(0, 0)));
    }

    [Fact]
    public void Given_NoPaLeftButActing_When_Decided_Then_NeverReturnsMoveOrAttack()
    {
        // Estado artificial com PA 0 em fase Acting: só Pass/Discard/Equip-nada são válidos.
        var state = Rolled(new FixedRandom(1), HunterAt(1, 0, 0), HunterAt(2, 4, 0)) with { ActionPoints = 0 };

        var action = Decide(state, 1);

        Assert.True(action is Pass or Discard, action.GetType().Name);
    }

    [Fact]
    public void Given_HunterView_When_OtherHunterInFog_Then_PositionHiddenUnlessMarked()
    {
        var state = NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), Treasure,
                HunterAt(1, 0, 0), HunterAt(2, 4, 4), HunterAt(3, 4, 0, hand: new[] { Treasure }))
            .StartAndRoll(new FixedRandom(4));

        var view = HunterView.For(state, 1, TestContent.Default);

        var hidden = view.Others.Single(o => o.Id == 2);
        var marked = view.Others.Single(o => o.Id == 3);
        Assert.Null(hidden.Position);
        Assert.Equal(new Position(4, 0), marked.Position);
        Assert.True(marked.IsMarked);
    }

    [Fact]
    public void Given_HunterView_When_MonsterOutOfSight_Then_NotListed()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0), HunterAt(2, 4, 4))
            .WithMonsters(MonsterAt(1, "kobold", 4, 0), MonsterAt(2, "kobold", 1, 1))
            .StartAndRoll(new FixedRandom(4));

        var view = HunterView.For(state, 1, TestContent.Default);

        Assert.Single(view.Monsters);
        Assert.Equal(2, view.Monsters[0].Id);
    }

    [Fact]
    public void Given_HunterView_When_ChestIsMimic_Then_SecretNotRevealed()
    {
        var state = NewGameWithChests(MapBuilder.WithChests5x5(), new[] { new Chest(new Position(1, 0), false, false, IsMimic: true) }, Treasure,
                HunterAt(1, 0, 0))
            .StartAndRoll(new FixedRandom(4));

        var view = HunterView.For(state, 1, TestContent.Default);

        Assert.False(view.Chests.Single().IsMimic);
    }
}
