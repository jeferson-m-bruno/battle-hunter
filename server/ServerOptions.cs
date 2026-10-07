namespace BattleHunter.Server;

/// <summary>Parâmetros do servidor (seção "BattleHunter" do appsettings). Os testes encurtam filas e atrasos.</summary>
public sealed class ServerOptions
{
    public const string Section = "BattleHunter";

    /// <summary>Segundos sem sala cheia até a IA completar as vagas (GDD: 30).</summary>
    public int QueueFillSeconds { get; set; } = 30;

    /// <summary>Timer do turno humano (GDD: 45).</summary>
    public int TurnSeconds { get; set; } = 45;

    /// <summary>Tolerância de reconexão antes de a IA assumir o caçador (GDD: 60).</summary>
    public int ReconnectGraceSeconds { get; set; } = 60;

    /// <summary>Atraso entre ações da IA e dos monstros, para os clientes acompanharem.</summary>
    public int AiDelayMs { get; set; } = 400;

    public int MatchmakingTickMs { get; set; } = 1000;

    /// <summary>Faixa de nível para partidas casuais (GDD: ±4).</summary>
    public int LevelRange { get; set; } = 4;

    /// <summary>Pasta data/ (padrão: procurada acima do executável).</summary>
    public string? DataDirectory { get; set; }
}
