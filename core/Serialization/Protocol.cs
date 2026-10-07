using System.Collections.Generic;
using BattleHunter.Core.Progression;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Serialization;

/// <summary>Mensagem do cliente para o servidor (WebSocket, JSON com "type").</summary>
public abstract record ClientMessage;

/// <summary>Login anônimo por dispositivo (GDD v1).</summary>
public sealed record Auth(string DeviceId, string Name) : ClientMessage;

/// <summary>Entra na fila de uma missão: "easy", "normal", "hard" ou "ranked".</summary>
public sealed record QueueJoin(string Mission) : ClientMessage;

public sealed record QueueLeave : ClientMessage;

/// <summary>Intenção na partida atual; o servidor valida pelo Reducer.</summary>
public sealed record PlayerAction(GameAction Action) : ClientMessage;

public sealed record Ping(long Sent) : ClientMessage;

// ---- guilda (fora de partida) ----

public sealed record ProfileRequest : ClientMessage;

public sealed record SetAppearance(string Name, int ColorIndex, int FaceIndex) : ClientMessage;

/// <summary>Gasta 1 ponto livre: "hp", "atk", "def", "spd" ou "luck".</summary>
public sealed record AllocatePoint(string Stat) : ClientMessage;

/// <summary>Define equipamento e as cartas levadas (até 5), todos do inventário.</summary>
public sealed record SetLoadout(Loadout Loadout) : ClientMessage;

public sealed record ShopRequest : ClientMessage;

public sealed record Buy(string CardId) : ClientMessage;

public sealed record Sell(string CardId) : ClientMessage;

public sealed record Deposit(int Amount) : ClientMessage;

public sealed record RankingRequest : ClientMessage;

/// <summary>Mensagem do servidor para o cliente.</summary>
public abstract record ServerMessage;

public sealed record Welcome(string PlayerId, string Name, int Level) : ServerMessage;

public sealed record Queued(string Mission, int Waiting, int SecondsUntilAiFill) : ServerMessage;

public sealed record QueueLeft : ServerMessage;

/// <summary>Sala formada: quem é quem (equipamentos visíveis, como no original) e o primeiro snapshot.</summary>
public sealed record MatchStarted(string RoomId, int HunterId, PlayerSnapshot Snapshot) : ServerMessage;

/// <summary>Eventos filtrados para este jogador mais o snapshot resultante.</summary>
public sealed record MatchUpdate(IReadOnlyList<GameEvent> Events, PlayerSnapshot Snapshot) : ServerMessage;

/// <summary>Reconexão: o estado inteiro visível, sem eventos.</summary>
public sealed record MatchResumed(string RoomId, int HunterId, PlayerSnapshot Snapshot) : ServerMessage;

public sealed record Rejected(string Reason) : ServerMessage;

public sealed record GameOver(GameEndReason Reason, int? WinnerId, PlayerSnapshot Snapshot) : ServerMessage;

public sealed record Pong(long Sent, long ServerTime) : ServerMessage;

public sealed record Error(string Message) : ServerMessage;

/// <summary>Perfil atual (após qualquer operação de guilda) e o XP que falta para o próximo nível.</summary>
public sealed record ProfileState(Profile Profile, int XpToNextLevel) : ServerMessage;

public sealed record ShopState(string Day, IReadOnlyList<string> Stock) : ServerMessage;

public sealed record RankingEntry(string PlayerId, string Name, int Level, int RankPoints);

public sealed record Ranking(string Season, IReadOnlyList<RankingEntry> Entries) : ServerMessage;

/// <summary>Depois de GameOver: o que a partida rendeu e o perfil já atualizado.</summary>
public sealed record MatchRewarded(RewardSummary Summary, Profile Profile) : ServerMessage;
