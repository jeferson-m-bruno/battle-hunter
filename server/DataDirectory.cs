namespace BattleHunter.Server;

/// <summary>Localiza a pasta data/ do repositório (ou BH_DATA_DIR) subindo a partir do executável.</summary>
public static class DataDirectory
{
    public static string Find()
    {
        var env = Environment.GetEnvironmentVariable("BH_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, "cards.json")))
            return env;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var data = Path.Combine(dir.FullName, "data");
            if (File.Exists(Path.Combine(data, "cards.json")))
                return data;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Pasta data/ não encontrada; defina BH_DATA_DIR.");
    }
}
