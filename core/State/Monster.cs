namespace BattleHunter.Core.State;

/// <summary>Máquina de 3 estados dos monstros (GDD, seção IA).</summary>
public enum MonsterState
{
    Idle,
    Chasing,
    Attacking,
}

/// <summary>Um monstro vivo no mapa. Os atributos fixos vêm do MonsterType em data/monsters.json.</summary>
/// <param name="SkipsNextAction">Preso numa Rede: não age na próxima fase dos monstros.</param>
/// <param name="ActionsTaken">Fases em que agiu; o chefe sopra a cada 3.</param>
/// <param name="State">Estado da FSM; muda por distância e linha de visão.</param>
public sealed record Monster(
    int Id,
    string TypeId,
    int Hp,
    Position Position,
    bool SkipsNextAction = false,
    int ActionsTaken = 0,
    MonsterState State = MonsterState.Idle);
