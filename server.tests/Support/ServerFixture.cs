using BattleHunter.Server.Matchmaking;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BattleHunter.Server.Tests.Support;

/// <summary>Servidor em memória com fila instantânea e IA sem atraso; os testes forçam o tique do matchmaking.</summary>
public sealed class ServerFixture : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ServerFixture()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("BattleHunter:QueueFillSeconds", "0");
            builder.UseSetting("BattleHunter:AiDelayMs", "0");
            builder.UseSetting("BattleHunter:MatchmakingTickMs", "60000"); // os testes chamam TickAsync; o tique de fundo não pode formar salas no meio de um teste
            builder.UseSetting("BattleHunter:TurnSeconds", "45");
            builder.UseSetting("BattleHunter:ReconnectGraceSeconds", "60");
            builder.UseSetting("ConnectionStrings:Postgres", "");
            builder.UseSetting("BattleHunter:DataDirectory", FindData());
        });
        _ = _factory.Server;
    }

    public TestServer Server => _factory.Server;
    public HttpClient Http => _factory.CreateClient();
    public MatchmakingService Matchmaking => _factory.Services.GetRequiredService<MatchmakingService>();

    public Task<TestClient> ClientAsync(string deviceId, string name) => TestClient.ConnectAsync(Server, deviceId, name);

    public static string FindData()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var data = Path.Combine(dir.FullName, "data");
            if (File.Exists(Path.Combine(data, "cards.json")))
                return data;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("data/ não encontrada");
    }

    public void Dispose() => _factory.Dispose();
}
