using BattleHunter.Core.Cards;

namespace BattleHunter.Core.Tests.Support;

/// <summary>Conteúdo real de data/ (cards.json, loot_tables.json), localizado subindo a partir do diretório do teste.</summary>
public static class TestContent
{
    private static readonly Lazy<GameContent> Lazy = new(() => GameContent.LoadFromDirectory(DataDirectory));

    public static GameContent Default => Lazy.Value;

    public static string DataDirectory
    {
        get
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
    }
}
