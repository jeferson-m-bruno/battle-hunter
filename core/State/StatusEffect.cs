namespace BattleHunter.Core.State;

public enum StatusKind
{
    /// <summary>−2 PV no início de cada turno, por 3 turnos ou até Antídoto.</summary>
    Poisoned,
    /// <summary>Perde o próximo turno (Rede).</summary>
    Trapped,
}

/// <summary>Um estado ativo num caçador (GDD, tabela de estados). Lento vem dos modificadores; Marcado é derivado da mão.</summary>
public sealed record StatusEffect(StatusKind Kind, int TurnsLeft);
