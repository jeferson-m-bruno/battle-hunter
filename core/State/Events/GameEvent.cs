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

public sealed record GameEnded(GameEndReason Reason) : GameEvent;

/// <summary>A ação foi recusada; o estado não mudou.</summary>
public sealed record ActionRejected(GameAction Action, string Reason) : GameEvent;
