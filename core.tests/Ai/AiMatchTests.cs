using System.Diagnostics;
using BattleHunter.Core.Ai;
using BattleHunter.Core.State;
using BattleHunter.Core.Tests.Support;

namespace BattleHunter.Core.Tests.Ai;

public class AiMatchTests
{
    [Fact]
    public void Given_TwoHundredSeeds_When_Played_Then_EveryMatchFinishesWithoutExceptions()
    {
        var sw = Stopwatch.StartNew();
        var results = Enumerable.Range(1, 200).Select(seed => AiMatch.Play(seed, MatchSettings.Easy, TestContent.Default)).ToList();
        sw.Stop();

        Assert.All(results, r => Assert.False(r.Aborted, $"seed {r.Seed} abortou após {r.Actions} ações"));
        Assert.All(results, r => Assert.InRange(r.Rounds, 1, 30));
        Assert.True(sw.Elapsed.TotalSeconds < 60, $"200 partidas levaram {sw.Elapsed.TotalSeconds:F1}s");
    }

    [Fact]
    public void Given_SameSeed_When_PlayedTwice_Then_SameResult()
    {
        var a = AiMatch.Play(77, MatchSettings.Normal, TestContent.Default);
        var b = AiMatch.Play(77, MatchSettings.Normal, TestContent.Default);

        Assert.Equal(a, b with { Profiles = a.Profiles });
        Assert.Equal(a.Rounds, b.Rounds);
        Assert.Equal(a.WinnerId, b.WinnerId);
    }

    [Fact]
    public void Given_ManySeeds_When_Played_Then_SomeoneExtractsTheTreasureSometimes()
    {
        var results = Enumerable.Range(1, 100).Select(seed => AiMatch.Play(seed, MatchSettings.Easy, TestContent.Default)).ToList();

        Assert.Contains(results, r => r.Reason == GameEndReason.TreasureExtracted);
        Assert.All(results.Where(r => r.Reason == GameEndReason.TreasureExtracted), r => Assert.NotNull(r.WinnerProfile));
    }

    [Fact]
    public void Given_AiPlayers_When_Played_Then_RejectionsAreRare()
    {
        var results = Enumerable.Range(1, 50).Select(seed => AiMatch.Play(seed, MatchSettings.Easy, TestContent.Default)).ToList();

        var total = results.Sum(r => r.Actions);
        var rejected = results.Sum(r => r.Rejections);
        Assert.True(rejected * 100 < total * 2, $"{rejected} recusas em {total} ações");
    }
}
