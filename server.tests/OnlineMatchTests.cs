using System.Net.Http.Json;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using BattleHunter.Server.Tests.Support;

namespace BattleHunter.Server.Tests;

public class OnlineMatchTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _server;

    public OnlineMatchTests(ServerFixture server) => _server = server;

    private string Device(string tag) => $"{tag}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Given_FourClients_When_Queued_Then_SameRoomAndEveryoneReachesGameOver()
    {
        var clients = new List<TestClient>();
        for (var i = 1; i <= 4; i++)
            clients.Add(await _server.ClientAsync(Device("quad"), $"J{i}"));

        foreach (var c in clients)
        {
            await c.SendAsync(new QueueJoin("easy"));
            await c.WaitForAsync<Queued>();
        }
        await _server.Matchmaking.TickAsync();

        var starts = new List<MatchStarted>();
        foreach (var c in clients)
            starts.Add(await c.WaitForAsync<MatchStarted>());

        Assert.Single(starts.Select(s => s.RoomId).Distinct());
        Assert.Equal(new[] { 1, 2, 3, 4 }, starts.Select(s => s.HunterId).OrderBy(x => x));
        Assert.All(starts, s => Assert.Equal(4, s.Snapshot.Hunters.Count));
        Assert.All(starts, s => Assert.Equal(s.HunterId, s.Snapshot.Self.Id));

        await Task.WhenAll(clients.Select(c => c.PlayPassingUntilOverAsync()));

        var overs = clients.Select(c => c.Received.OfType<GameOver>().Single()).ToList();
        Assert.Single(overs.Select(o => o.Reason).Distinct());
        Assert.All(overs, o => Assert.Equal(GamePhase.Finished, o.Snapshot.Phase));

        foreach (var c in clients)
            await c.DisposeAsync();
    }

    [Fact]
    public async Task Given_OneClient_When_Queued_Then_AiFillsTheRoomAndMatchProceeds()
    {
        await using var client = await _server.ClientAsync(Device("solo"), "Solo");
        await client.SendAsync(new QueueJoin("easy"));
        var queued = await client.WaitForAsync<Queued>();
        Assert.Equal("easy", queued.Mission);

        await _server.Matchmaking.TickAsync();
        var started = await client.WaitForAsync<MatchStarted>();

        Assert.Equal(4, started.Snapshot.Hunters.Count);
        Assert.Equal(3, started.Snapshot.Hunters.Count(h => h.Name.StartsWith("IA ")));

        await client.PlayPassingUntilOverAsync();
        Assert.Single(client.Received.OfType<GameOver>());
    }

    [Fact]
    public async Task Given_ActionOutOfTurnOrForAnotherHunter_When_Sent_Then_Rejected()
    {
        await using var a = await _server.ClientAsync(Device("turn-a"), "A");
        await using var b = await _server.ClientAsync(Device("turn-b"), "B");
        await a.SendAsync(new QueueJoin("normal"));
        await a.WaitForAsync<Queued>();
        await b.SendAsync(new QueueJoin("normal"));
        await b.WaitForAsync<Queued>();
        await _server.Matchmaking.TickAsync();
        var sa = await a.WaitForAsync<MatchStarted>();
        var sb = await b.WaitForAsync<MatchStarted>();

        var (current, other) = sa.Snapshot.CurrentHunterId == sa.HunterId ? (a, b) : (b, a);

        await other.ActAsync(new RollDice(other.HunterId));
        var rejected = await other.WaitForAsync<Rejected>();
        Assert.Contains("vez", rejected.Reason);

        await current.ActAsync(new RollDice(other.HunterId));
        var forged = await current.WaitForAsync<Rejected>(r => r.Reason.Contains("seu caçador"));
        Assert.NotNull(forged);

        await current.ActAsync(new RollDice(current.HunterId));
        var update = await current.WaitForAsync<MatchUpdate>(u => u.Events.Any(e => e is DiceRolled));
        Assert.Equal(GamePhase.Acting, update.Snapshot.Phase);
    }

    [Fact]
    public async Task Given_ClientDisconnects_When_ReconnectsWithSameDevice_Then_ReceivesFullSnapshot()
    {
        var device = Device("reconnect");
        var first = await _server.ClientAsync(device, "Volto");
        await first.SendAsync(new QueueJoin("easy"));
        await first.WaitForAsync<Queued>();
        await _server.Matchmaking.TickAsync();
        var started = await first.WaitForAsync<MatchStarted>();
        await first.CloseAsync();

        await using var second = await _server.ClientAsync(device, "Volto");
        var resumed = await second.WaitForAsync<MatchResumed>();

        Assert.Equal(started.RoomId, resumed.RoomId);
        Assert.Equal(started.HunterId, resumed.HunterId);
        Assert.Equal(started.Snapshot.Self.Name, resumed.Snapshot.Self.Name);
        Assert.Equal(started.Snapshot.Map.Cells, resumed.Snapshot.Map.Cells);
        Assert.True(resumed.Snapshot.Round >= started.Snapshot.Round);
    }

    [Fact]
    public async Task Given_TwoClients_When_Playing_Then_NobodyReceivesAnotherHand()
    {
        await using var a = await _server.ClientAsync(Device("hide-a"), "A");
        await using var b = await _server.ClientAsync(Device("hide-b"), "B");
        await a.SendAsync(new QueueJoin("easy"));
        await a.WaitForAsync<Queued>();
        await b.SendAsync(new QueueJoin("easy"));
        await b.WaitForAsync<Queued>();
        await _server.Matchmaking.TickAsync();
        await a.WaitForAsync<MatchStarted>();
        await b.WaitForAsync<MatchStarted>();

        await Task.WhenAll(a.PlayPassingUntilOverAsync(), b.PlayPassingUntilOverAsync());

        foreach (var client in new[] { a, b })
        {
            foreach (var m in client.Received)
            {
                var snapshot = m switch
                {
                    MatchStarted s => s.Snapshot,
                    MatchUpdate u => u.Snapshot,
                    GameOver g => g.Snapshot,
                    _ => null,
                };
                if (snapshot == null)
                    continue;

                Assert.Equal(client.HunterId, snapshot.Self.Id);
                Assert.Equal(client.HunterId, snapshot.HunterId);

                if (m is MatchUpdate mu)
                    Assert.DoesNotContain(mu.Events, e => e is CardDrawn d && d.HunterId != client.HunterId);
            }
        }
    }

    [Fact]
    public async Task Given_NoAuth_When_Queueing_Then_Error()
    {
        var ws = await _server.Server.CreateWebSocketClient().ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        var json = System.Text.Encoding.UTF8.GetBytes(MessageJson.Serialize<ClientMessage>(new QueueJoin("easy")));
        await ws.SendAsync(json, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[8192];
        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        var reply = MessageJson.Deserialize<ServerMessage>(System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count));

        var error = Assert.IsType<Error>(reply);
        Assert.Contains("Auth", error.Message);
        ws.Dispose();
    }

    [Fact]
    public async Task Given_Server_When_HealthAndMetricsRequested_Then_Ok()
    {
        var health = await _server.Http.GetFromJsonAsync<Dictionary<string, string>>("/health");
        Assert.Equal("ok", health!["status"]);
        Assert.Equal("InMemoryPlayerStore", health["store"]);

        var metrics = await _server.Http.GetFromJsonAsync<Dictionary<string, object>>("/metrics");
        Assert.True(metrics!.ContainsKey("roomsActive"));
    }
}
