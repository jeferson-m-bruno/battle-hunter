using BattleHunter.Core.Progression;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Server.Tests.Support;

namespace BattleHunter.Server.Tests;

public class GuildTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _server;

    public GuildTests(ServerFixture server) => _server = server;

    private string Device(string tag) => $"{tag}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Given_NewPlayer_When_ProfileRequested_Then_Level1NoGoldEmptyInventory()
    {
        await using var c = await _server.ClientAsync(Device("profile"), "Novato");
        await c.SendAsync(new ProfileRequest());

        var state = await c.WaitForAsync<ProfileState>();

        Assert.Equal("Novato", state.Profile.Name);
        Assert.Equal(1, state.Profile.Level);
        Assert.Equal(0, state.Profile.Gold);
        Assert.Empty(state.Profile.Inventory);
        Assert.Equal(100, state.XpToNextLevel);
    }

    [Fact]
    public async Task Given_LevelOne_When_QueueingNormal_Then_ErrorRequiresLevel5()
    {
        await using var c = await _server.ClientAsync(Device("locked"), "Baixo");
        await c.SendAsync(new QueueJoin("normal"));

        var error = await c.WaitForAsync<Error>();

        Assert.Contains("nível 5", error.Reason());
    }

    [Fact]
    public async Task Given_NoFreePoints_When_Allocating_Then_Error()
    {
        await using var c = await _server.ClientAsync(Device("points"), "Zero");
        await c.SendAsync(new AllocatePoint("atk"));

        var error = await c.WaitForAsync<Error>();

        Assert.Contains("Sem pontos", error.Message);
    }

    [Fact]
    public async Task Given_ShopAndNoGold_When_Buying_Then_StockListedButPurchaseRefused()
    {
        await using var c = await _server.ClientAsync(Device("shop"), "Pobre");
        await c.SendAsync(new ShopRequest());
        var shop = await c.WaitForAsync<ShopState>();
        Assert.Equal(9, shop.Stock.Count);

        await c.SendAsync(new Buy(shop.Stock[0]));
        var error = await c.WaitForAsync<Error>();
        Assert.Contains("comprar", error.Message);
    }

    [Fact]
    public async Task Given_LoadoutWithCardNotOwned_When_Set_Then_Error()
    {
        await using var c = await _server.ClientAsync(Device("loadout"), "Sem");
        await c.SendAsync(new SetLoadout(new Loadout(Equipment.None, new[] { "axe" })));

        var error = await c.WaitForAsync<Error>();

        Assert.Contains("inventário", error.Message);
    }

    [Fact]
    public async Task Given_Appearance_When_Set_Then_ProfileUpdated()
    {
        await using var c = await _server.ClientAsync(Device("face"), "Antes");
        await c.SendAsync(new SetAppearance("Depois", 2, 3));

        var state = await c.WaitForAsync<ProfileState>();

        Assert.Equal(("Depois", 2, 3), (state.Profile.Name, state.Profile.ColorIndex, state.Profile.FaceIndex));
    }

    [Fact]
    public async Task Given_MatchPlayed_When_Over_Then_MatchRewardedUpdatesProfileAndLoadoutCanUseWonCards()
    {
        await using var c = await _server.ClientAsync(Device("reward"), "Ganha");
        await c.SendAsync(new QueueJoin("easy"));
        await c.WaitForAsync<Queued>();
        await _server.Matchmaking.TickAsync();
        await c.WaitForAsync<MatchStarted>();

        await c.PlayPassingUntilOverAsync();
        var rewarded = await c.WaitForAsync<MatchRewarded>();

        Assert.True(rewarded.Summary.XpGained >= 0);
        Assert.Equal(rewarded.Profile.Xp, rewarded.Summary.XpGained);
        Assert.True(rewarded.Profile.Gold >= 0);
        Assert.Equal(rewarded.Profile.Inventory.Count, rewarded.Summary.CardsKept.Count + (rewarded.Summary.RareCard != null ? 1 : 0));

        await c.SendAsync(new ProfileRequest());
        var state = await c.WaitForAsync<ProfileState>();
        Assert.Equal(rewarded.Profile.Xp, state.Profile.Xp);

        await c.SendAsync(new RankingRequest());
        var ranking = await c.WaitForAsync<Ranking>();
        Assert.Equal(DateTimeOffset.UtcNow.ToString("yyyy-MM"), ranking.Season);
    }
}

internal static class ErrorExtensions
{
    public static string Reason(this Error e) => e.Message;
}
