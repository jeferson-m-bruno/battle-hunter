using System.Collections.Generic;
using BattleHunter.Core.State.Actions;

namespace BattleHunter.Core.State.Events;

/// <summary>Fato ocorrido na partida. O cliente só renderiza eventos; nunca recalcula.</summary>
public abstract record GameEvent;

public sealed record GameStarted(IReadOnlyList<int> TurnOrder) : GameEvent;

public sealed record TurnStarted(int HunterId, int Round) : GameEvent;

public sealed record DiceRolled(int HunterId, int Die, int SpeedBonus, int ActionPoints) : GameEvent;

public sealed record HunterMoved(int HunterId, Position From, Position To, int ActionPointsLeft) : GameEvent;

public sealed record TurnEnded(int HunterId) : GameEvent;

public sealed record HunterExited(int HunterId) : GameEvent;

public sealed record GameEnded(GameEndReason Reason, int? WinnerId) : GameEvent;

public sealed record ChestOpened(int HunterId, Position ChestPosition, int ActionPointsLeft) : GameEvent;

/// <summary>Informação privada: o servidor só envia ao dono da mão.</summary>
public sealed record CardDrawn(int HunterId, string CardId) : GameEvent;

public sealed record CardDiscarded(int HunterId, string CardId) : GameEvent;

public sealed record CardEquipped(int HunterId, string CardId, EquipmentSlot Slot, string? UnequippedCardId, int ActionPointsLeft) : GameEvent;

/// <summary>O caçador passou a carregar o tesouro-alvo; visível a todos.</summary>
public sealed record HunterMarked(int HunterId) : GameEvent;

/// <summary>Resultado de um ataque: os dados rolados, para o cliente animar; o dano já aplicado.</summary>
public sealed record AttackResolved(
    Combatant Attacker,
    Combatant Target,
    int AttackerDie,
    int DefenderDie,
    int Damage,
    bool Critical,
    bool Dodged,
    int TargetHpLeft) : GameEvent;

public sealed record MonsterSpawned(int MonsterId, string TypeId, Position Position) : GameEvent;

public sealed record MonsterMoved(int MonsterId, Position From, Position To) : GameEvent;

public sealed record MonsterStateChanged(int MonsterId, MonsterState From, MonsterState To) : GameEvent;

public sealed record MonsterDefeated(int MonsterId, string TypeId, int KillerHunterId, int XpGained) : GameEvent;

/// <summary>O caçador caiu (0 PV): sai da partida e a mão inteira fica no chão da célula.</summary>
public sealed record HunterFell(int HunterId, Combatant KilledBy, Position Position, IReadOnlyList<string> DroppedCards) : GameEvent;

/// <summary>O atacante roubou uma carta da mão do alvo após o dano. O id da carta só vai aos dois envolvidos.</summary>
public sealed record CardStolen(int ThiefId, int VictimId, string CardId) : GameEvent;

public sealed record CardPickedUp(int HunterId, string CardId, int ActionPointsLeft) : GameEvent;

/// <summary>Carta ativa usada e descartada; Target é a célula-alvo quando o efeito pede.</summary>
public sealed record CardUsed(int HunterId, string CardId, Position? Target, int ActionPointsLeft) : GameEvent;

/// <summary>Dano sem rolagem (bomba, arremesso, armadilha, veneno). Cause = id da carta ou "poison".</summary>
public sealed record DamageDealt(Combatant Source, Combatant Target, int Damage, int HpLeft, string Cause) : GameEvent;

public sealed record HunterHealed(int HunterId, int Amount, int HpLeft) : GameEvent;

public sealed record ActionPointsGained(int HunterId, int Amount, int ActionPointsLeft) : GameEvent;

public sealed record StatusApplied(int HunterId, StatusKind Kind, int Turns) : GameEvent;

public sealed record StatusRemoved(int HunterId, StatusKind Kind) : GameEvent;

/// <summary>Caçador preso perdeu o turno.</summary>
public sealed record TurnSkipped(int HunterId) : GameEvent;

/// <summary>Informação privada do dono: a armadilha fica invisível aos outros até disparar.</summary>
public sealed record TrapPlaced(int OwnerId, Position Position, string CardId) : GameEvent;

public sealed record TrapTriggered(Position Position, string CardId, int OwnerId, Combatant Victim) : GameEvent;

public sealed record MonsterEntangled(int MonsterId) : GameEvent;

public sealed record AlarmRaised(Position Position, int Rounds) : GameEvent;

/// <summary>Cartas derrubadas da mão (Rasteira) caem no chão da célula do alvo.</summary>
public sealed record CardsDropped(int HunterId, Position Position, IReadOnlyList<string> Cards) : GameEvent;

/// <summary>O chefe entrou no mapa (sala da saída).</summary>
public sealed record BossAppeared(int MonsterId, Position Position) : GameEvent;

/// <summary>Sopro do chefe: as células atingidas; o dano vem em DamageDealt com causa "breath".</summary>
public sealed record BossBreath(int MonsterId, IReadOnlyList<Position> Cells) : GameEvent;

/// <summary>Fase dos monstros encerrada (fim da rodada).</summary>
public sealed record MonsterPhaseEnded(int Round) : GameEvent;

/// <summary>A ação foi recusada; o estado não mudou.</summary>
public sealed record ActionRejected(GameAction Action, string Reason) : GameEvent;
