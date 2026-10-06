using BattleHunter.Core.Rules;

namespace BattleHunter.Core.Tests.Rules;

public class SeededRandomTests
{
    [Fact]
    public void Given_SameSeed_When_RollingD6_Then_SequencesAreIdentical()
    {
        var a = new SeededRandom(42);
        var b = new SeededRandom(42);

        var rollsA = Enumerable.Range(0, 100).Select(_ => a.NextD6()).ToArray();
        var rollsB = Enumerable.Range(0, 100).Select(_ => b.NextD6()).ToArray();

        Assert.Equal(rollsA, rollsB);
    }

    [Fact]
    public void Given_DifferentSeeds_When_RollingD6_Then_SequencesDiffer()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);

        var rollsA = Enumerable.Range(0, 50).Select(_ => a.NextD6()).ToArray();
        var rollsB = Enumerable.Range(0, 50).Select(_ => b.NextD6()).ToArray();

        Assert.NotEqual(rollsA, rollsB);
    }

    [Fact]
    public void Given_ManyRolls_When_RollingD6_Then_AllValuesAreBetween1And6()
    {
        var random = new SeededRandom(7);

        var rolls = Enumerable.Range(0, 10_000).Select(_ => random.NextD6()).ToArray();

        Assert.All(rolls, r => Assert.InRange(r, 1, 6));
        Assert.Equal(6, rolls.Distinct().Count());
    }

    [Fact]
    public void Given_Next_When_CalledWithMax_Then_ValueIsBelowMax()
    {
        var random = new SeededRandom(3);

        var values = Enumerable.Range(0, 1_000).Select(_ => random.Next(4)).ToArray();

        Assert.All(values, v => Assert.InRange(v, 0, 3));
    }

    [Fact]
    public void Given_SeedZero_When_Rolling_Then_StillProducesVariedValues()
    {
        var random = new SeededRandom(0);

        var rolls = Enumerable.Range(0, 100).Select(_ => random.NextD6()).ToArray();

        Assert.True(rolls.Distinct().Count() > 1);
    }
}
