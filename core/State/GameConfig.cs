namespace BattleHunter.Core.State;

/// <summary>Parâmetros da missão que as regras leem. Os valores por tipo de missão vêm de data/missions.json (fatia 7).</summary>
/// <param name="MaxRounds">Limite de rodadas; ao estourar, missão falha para todos.</param>
/// <param name="MimicChancePercent">Chance (0–100) de cada baú comum ser um Mímico.</param>
/// <param name="MonsterSpawnInterval">A cada N rodadas nasce um monstro novo num spawn aleatório.</param>
/// <param name="BossRound">Rodada em que o chefe aparece (ou antes, ao pegar o tesouro-alvo).</param>
public sealed record GameConfig(
    int MaxRounds = 30,
    int MimicChancePercent = 15,
    int MonsterSpawnInterval = 5,
    int BossRound = 15);
