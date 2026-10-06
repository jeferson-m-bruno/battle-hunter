using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Core.Tests.Support;
using static BattleHunter.Core.Tests.Support.GameFixture;

namespace BattleHunter.Core.Tests.Rules;

public class StealRulesTests
{
    private const string Treasure = "treasure_dragon_eye";
    private static readonly Position VictimCell = new(1, 0);

    /// <summary>Caçador 1 em (0,0) ataca o caçador 2 em (1,0). Dados: atacante, defensor, [teste de roubo: vítima, ladrão], [índice].</summary>
    private static GameState Duel(IReadOnlyList<string> victimHand, string? treasureId = null) =>
        NewGameWithChests(MapBuilder.Open5x5(), Array.Empty<Chest>(), treasureId,
                HunterAt(1, 0, 0), HunterAt(2, 1, 0, hand: victimHand))
            .StartAndRoll(new FixedRandom(5));

    [Fact]
    public void Given_VictimFailsLuckTest_When_Hit_Then_OneCardIsStolen()
    {
        // Dano: 4+3 − (2+1) = 4. Roubo: vítima 1+1 = 2 < ladrão 4+1 = 5 -> rouba. Índice: Next(2) = 1 -> "boots".
        var state = Duel(new[] { "dagger", "boots" });

        var result = state.Apply(new Attack(1, VictimCell), new FixedRandom(3, 1, 1, 4));

        var stolen = Assert.Single(result.Events.OfType<CardStolen>());
        Assert.Equal((1, 2, "boots"), (stolen.ThiefId, stolen.VictimId, stolen.CardId));
        Assert.Equal(new[] { "dagger" }, result.State.Hunter(2).Hand);
        Assert.Equal(new[] { "boots" }, result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_VictimPassesLuckTest_When_Hit_Then_NothingStolen()
    {
        // Vítima 4+1 = 5 >= ladrão 3+1 = 4 -> não rouba (empate também protege a vítima).
        var state = Duel(new[] { "dagger", "boots" });

        var result = state.Apply(new Attack(1, VictimCell), new FixedRandom(3, 1, 4, 3));

        Assert.Empty(result.Events.OfType<CardStolen>());
        Assert.Equal(2, result.State.Hunter(2).Hand.Count);
    }

    [Fact]
    public void Given_VictimHoldsTreasure_When_Stolen_Then_TreasureHasPriorityAndThiefIsMarked()
    {
        var state = Duel(new[] { "dagger", Treasure, "boots" }, Treasure);
        Assert.True(state.IsMarked(2));

        var result = state.Apply(new Attack(1, VictimCell), new FixedRandom(3, 1, 1, 6));

        var stolen = Assert.Single(result.Events.OfType<CardStolen>());
        Assert.Equal(Treasure, stolen.CardId);
        Assert.True(result.State.IsMarked(1));
        Assert.False(result.State.IsMarked(2));
        Assert.Contains(result.Events, e => e is HunterMarked m && m.HunterId == 1);
    }

    [Fact]
    public void Given_AttackDodged_When_Resolved_Then_NoStealTest()
    {
        // Defensor rola 6 e esquiva (1 ≤ SOR 1): dano zero, nenhum dado de roubo consumido.
        var state = Duel(new[] { "dagger" });

        var result = state.Apply(new Attack(1, VictimCell), new FixedRandom(3, 6, 1));

        Assert.Empty(result.Events.OfType<CardStolen>());
        Assert.Single(result.State.Hunter(2).Hand);
    }

    [Fact]
    public void Given_VictimFalls_When_Hit_Then_CardsDropInsteadOfBeingStolen()
    {
        var victim = HunterAt(2, 1, 0, hand: new[] { "dagger" }) with { Hp = 1 };
        var state = Rolled(new FixedRandom(5), HunterAt(1, 0, 0), victim);

        var result = state.Apply(new Attack(1, VictimCell), new FixedRandom(3, 1, 1, 6));

        Assert.Empty(result.Events.OfType<CardStolen>());
        Assert.Single(result.State.GroundCards, g => g.CardId == "dagger" && g.Position == VictimCell);
        Assert.Empty(result.State.Hunter(1).Hand);
    }

    [Fact]
    public void Given_EmptyHand_When_Hit_Then_NothingToSteal()
    {
        var state = Duel(Array.Empty<string>());

        var result = state.Apply(new Attack(1, VictimCell), new FixedRandom(3, 1));

        Assert.Empty(result.Events.OfType<CardStolen>());
    }

    [Fact]
    public void Given_MonsterAttacksHunter_When_Hit_Then_NoSteal()
    {
        var state = NewGame(MapBuilder.Open5x5(), HunterAt(1, 0, 0, hand: new[] { "dagger" }))
            .WithMonsters(MonsterAt(1, "kobold", 1, 0));
        var random = new FixedRandom(1, 4, 1, 1, 6);
        state = state.Apply(new StartGame(), random).State;
        state = state.Apply(new RollDice(1), random).State;

        var result = state.Apply(new Pass(1), random);

        Assert.Single(result.Events.OfType<AttackResolved>());
        Assert.Empty(result.Events.OfType<CardStolen>());
        Assert.Single(result.State.Hunter(1).Hand);
    }
}
