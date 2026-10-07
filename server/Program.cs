using BattleHunter.Core.Cards;
using BattleHunter.Server;
using BattleHunter.Server.Matchmaking;
using BattleHunter.Server.Net;
using BattleHunter.Server.Persistence;
using BattleHunter.Server.Rooms;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection(ServerOptions.Section));
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<ServerOptions>>().Value;
    return GameContent.LoadFromDirectory(options.DataDirectory ?? DataDirectory.Find());
});
builder.Services.AddSingleton<IRoomStore, InMemoryRoomStore>();
builder.Services.AddSingleton<IPlayerStore>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
        return new InMemoryPlayerStore();

    var store = new PostgresPlayerStore(connectionString);
    store.EnsureSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
    return store;
});
builder.Services.AddSingleton<MatchmakingService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MatchmakingService>());

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

app.MapGet("/", () => "Battle Hunter server. WebSocket em /ws, /health, /metrics.");
app.MapGet("/health", (IPlayerStore players) => Results.Ok(new { status = "ok", store = players.GetType().Name }));
app.MapGet("/metrics", (IRoomStore rooms) =>
{
    var uptimeHours = Math.Max(1.0 / 60, (DateTimeOffset.UtcNow - Metrics.StartedAt).TotalHours);
    return Results.Ok(new
    {
        roomsActive = rooms.All.Count(r => !r.IsFinished),
        roomsTotal = rooms.Count,
        matchesStarted = Metrics.MatchesStarted,
        matchesFinished = Metrics.MatchesFinished,
        matchesPerHour = Metrics.MatchesStarted / uptimeHours,
        connectionsOpened = Metrics.ConnectionsOpened,
        uptimeSeconds = (int)(DateTimeOffset.UtcNow - Metrics.StartedAt).TotalSeconds,
    });
});
app.Map("/ws", async (HttpContext http, IPlayerStore players, IRoomStore rooms, MatchmakingService matchmaking, GameContent content, ILoggerFactory logs) =>
    await WebSocketEndpoint.HandleAsync(http, players, rooms, matchmaking, content, logs.CreateLogger("ws")));

app.Run();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program
{
}
