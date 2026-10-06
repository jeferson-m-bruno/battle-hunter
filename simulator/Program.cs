using System.Collections.Concurrent;
using System.Diagnostics;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;

// Uso: dotnet run --project simulator -- [N] [--seed S] [--mission easy|normal|hard]
// Roda N partidas IA×IA sem renderização e imprime estatísticas de balanceamento.

var count = 1000;
var baseSeed = 1;
var mission = "easy";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length:
            baseSeed = int.Parse(args[++i]);
            break;
        case "--mission" when i + 1 < args.Length:
            mission = args[++i].ToLowerInvariant();
            break;
        default:
            if (int.TryParse(args[i], out var n))
                count = n;
            break;
    }
}

var settings = mission switch
{
    "normal" => MatchSettings.Normal,
    "hard" => MatchSettings.Hard,
    _ => MatchSettings.Easy,
};

var content = GameContent.LoadFromDirectory(FindDataDirectory());
var results = new ConcurrentBag<MatchResult>();
var sw = Stopwatch.StartNew();

Parallel.For(0, count, i => results.Add(AiMatch.Play(baseSeed + i, settings, content)));

sw.Stop();
var all = results.OrderBy(r => r.Seed).ToList();

Console.WriteLine($"Battle Hunter — simulação IA×IA");
Console.WriteLine($"Missão: {mission}  Partidas: {count}  Seeds: {baseSeed}..{baseSeed + count - 1}  Tempo: {sw.Elapsed.TotalSeconds:F1}s ({count / Math.Max(0.001, sw.Elapsed.TotalSeconds):F0}/s)");
Console.WriteLine();

Console.WriteLine("Vitórias por perfil (missão vencida = saiu com o tesouro):");
foreach (var profile in AiMatch.DefaultProfiles)
{
    var wins = all.Count(r => r.WinnerProfile == profile);
    Console.WriteLine($"  {profile,-11} {wins,5}  {Pct(wins, count),6}");
}

Console.WriteLine();
Console.WriteLine("Fim de partida:");
foreach (var reason in Enum.GetValues<GameEndReason>())
{
    var n = all.Count(r => r.Reason == reason && !r.Aborted);
    Console.WriteLine($"  {reason,-18} {n,5}  {Pct(n, count),6}");
}

var aborted = all.Count(r => r.Aborted);
if (aborted > 0)
    Console.WriteLine($"  {"Abortadas",-18} {aborted,5}  {Pct(aborted, count),6}");

Console.WriteLine();
Console.WriteLine($"Rodadas: média {all.Average(r => r.Rounds):F1}  mín {all.Min(r => r.Rounds)}  máx {all.Max(r => r.Rounds)}");
Console.WriteLine($"Caídos por partida: {all.Average(r => r.Fallen):F2}   Saíram: {all.Average(r => r.Exited):F2}");
Console.WriteLine($"Ações por partida: {all.Average(r => r.Actions):F0}   Recusas: {all.Sum(r => r.Rejections)} ({Pct(all.Sum(r => r.Rejections), all.Sum(r => r.Actions))})");

var maxShare = AiMatch.DefaultProfiles.Max(p => all.Count(r => r.WinnerProfile == p)) * 100.0 / count;
var avgRounds = all.Average(r => r.Rounds);
var ok = maxShare <= 35 && avgRounds >= 18 && avgRounds <= 25 && aborted == 0;
Console.WriteLine();
Console.WriteLine(ok
    ? "META: ok (nenhum perfil > 35%, média de rodadas em 18–25)"
    : $"META: fora (maior perfil {maxShare:F1}%, média {avgRounds:F1} rodadas)");

return ok ? 0 : 2;

static string Pct(int part, int total) => total == 0 ? "-" : $"{part * 100.0 / total:F1}%";

static string FindDataDirectory()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var data = Path.Combine(dir.FullName, "data");
        if (File.Exists(Path.Combine(data, "cards.json")))
            return data;
        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException("Pasta data/ não encontrada acima de " + AppContext.BaseDirectory);
}
