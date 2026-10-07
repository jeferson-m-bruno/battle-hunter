namespace BattleHunter.Core.State;

/// <summary>Parâmetros da missão que as regras leem. Os valores por tipo de missão vêm de data/missions.json (fatia 7).</summary>
/// <param name="MaxRounds">Limite de rodadas; ao estourar, missão falha para todos.</param>
/// <param name="MimicChancePercent">Chance (0–100) de cada baú comum ser um Mímico.</param>
/// <param name="MonsterSpawnInterval">A cada N rodadas nasce um monstro novo num spawn aleatório.</param>
/// <param name="BossRound">Rodada em que o chefe aparece (ou antes, ao pegar o tesouro-alvo); 0 = missão sem chefe.</param>
/// <param name="BossHpPercent">PV do chefe em % do valor da tabela (150 na missão difícil).</param>
/// <param name="ChestXp">XP por baú aberto (GDD: XP vem de baús abertos).</param>
/// <param name="ChestGoldPercent">Chance (0–100) de um baú comum dar ouro solto em vez de carta.</param>
/// <param name="ChestGoldMin">Ouro mínimo de um baú de ouro.</param>
/// <param name="ChestGoldMax">Ouro máximo de um baú de ouro.</param>
public sealed record GameConfig(
    int MaxRounds = 30,
    int MimicChancePercent = 15,
    int MonsterSpawnInterval = 5,
    int BossRound = 15,
    int BossHpPercent = 100,
    int ChestXp = 5,
    int ChestGoldPercent = 25,
    int ChestGoldMin = 15,
    int ChestGoldMax = 40);
