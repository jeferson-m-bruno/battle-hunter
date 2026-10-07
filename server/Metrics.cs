namespace BattleHunter.Server;

/// <summary>Contadores simples expostos em /metrics (GDD: salas ativas, partidas por hora).</summary>
public static class Metrics
{
    public static int MatchesStarted;
    public static int MatchesFinished;
    public static int ConnectionsOpened;
    public static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;
}
