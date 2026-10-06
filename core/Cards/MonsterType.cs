namespace BattleHunter.Core.Cards;

public enum MonsterBehavior
{
    /// <summary>Persegue o caçador mais próximo em linha de visão.</summary>
    ChaseNearest,
    /// <summary>Nunca sai da sala; ataca quem entra.</summary>
    GuardRoom,
    /// <summary>Persegue o caçador mais ferido.</summary>
    ChaseWounded,
    /// <summary>Parece baú; ataca ao ser aberto.</summary>
    Mimic,
    /// <summary>Chefe: sopro em linha a cada 3 turnos.</summary>
    Boss,
}

/// <summary>Uma linha de data/monsters.json. Monstros não têm SOR: nunca dão crítico nem esquivam.</summary>
public sealed record MonsterType(
    string Id,
    string Name,
    int Hp,
    int Atk,
    int Def,
    int Xp,
    string Loot,
    MonsterBehavior Behavior,
    bool Poison,
    bool Spawns);
