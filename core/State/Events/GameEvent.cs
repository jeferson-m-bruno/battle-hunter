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

/// <summary>A ação foi recusada; o estado não mudou.</summary>
public sealed record ActionRejected(GameAction Action, string Reason) : GameEvent;
