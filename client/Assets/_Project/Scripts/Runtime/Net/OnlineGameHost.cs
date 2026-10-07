using System;
using System.Collections;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using UnityEngine;

namespace BattleHunter.Client.Net
{
    /// <summary>
    /// Conexão online: guilda (perfil, loja, ranking) e partida. O servidor é a verdade: este host só envia intenções
    /// e traduz MatchUpdate em eventos + snapshot. Sobrevive à troca de cena e reconecta sozinho ao cair.
    /// </summary>
    public sealed class OnlineGameHost : MonoBehaviour, IGameHost
    {
        public enum Status { Disconnected, Connecting, Connected, Queued, InMatch, Finished }

        private const float ReconnectSeconds = 3f;

        public GameContent Content { get; private set; }
        public int HumanId { get; private set; }
        public PlayerSnapshot View { get; private set; }
        public bool IsHumanTurn => View != null && !View.IsFinished() && View.CurrentHunterId == HumanId;
        public float TurnTimeLeft { get; private set; } = 45f;
        public string Title => $"online · {GameSession.Mission} · sala {RoomId ?? "?"}";
        public Status State { get; private set; } = Status.Disconnected;
        public string StatusText { get; private set; } = "";
        public string RoomId { get; private set; }
        public string PlayerId { get; private set; }
        public int Waiting { get; private set; }
        public OnlineProfileService Guild { get; private set; }

        public event Action<GameEvent> OnEvent;
        public event Action OnStateChanged;
        public event Action<MatchOutcome> OnFinished;
        public event Action OnStatusChanged;
        public event Action<ServerMessage> OnGuildMessage;

        private WebSocketClient _socket;
        private string _url;
        private int _lastTurnKey = -1;
        private bool _wantReconnect;

        public static string DeviceId => SystemInfo.deviceUniqueIdentifier + "-" + Application.productName;

        /// <summary>Conecta e autentica; a fila é pedida à parte (JoinQueue).</summary>
        public void Connect(string url)
        {
            Content = ContentLoader.Load();
            Guild ??= new OnlineProfileService(this);
            _url = url;
            _wantReconnect = true;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(ConnectRoutine());
        }

        public void JoinQueue(string mission)
        {
            GameSession.Mission = mission;
            View = null;
            RoomId = null;
            Send(new QueueJoin(mission));
        }

        public void LeaveQueue()
        {
            Send(new QueueLeave());
            SetStatus(Status.Connected, "Fora da fila.");
        }

        public void Send(ClientMessage message) => _socket?.Send(message);

        public bool Submit(GameAction action)
        {
            if (!IsHumanTurn || _socket == null || !_socket.IsOpen)
                return false;

            Send(new PlayerAction(action));
            return true;
        }

        /// <summary>Volta ao estado de guilda depois de uma partida, mantendo a conexão.</summary>
        public void ResetMatch()
        {
            View = null;
            RoomId = null;
            _wantReconnect = true;
            SetStatus(_socket != null && _socket.IsOpen ? Status.Connected : Status.Disconnected, "");
        }

        public void Shutdown()
        {
            _wantReconnect = false;
            _socket?.Dispose();
            Destroy(gameObject);
        }

        private IEnumerator ConnectRoutine()
        {
            SetStatus(Status.Connecting, $"Conectando a {_url}…");
            _socket?.Dispose();
            _socket = new WebSocketClient();
            var task = _socket.ConnectAsync(_url);
            while (!task.IsCompleted)
                yield return null;

            if (!task.Result)
            {
                SetStatus(Status.Disconnected, "Não conectou: " + _socket.LastError);
                if (_wantReconnect && State != Status.Finished)
                {
                    yield return new WaitForSeconds(ReconnectSeconds);
                    StartCoroutine(ConnectRoutine());
                }
                yield break;
            }

            Send(new Auth(DeviceId, GameSession.HunterName));
            SetStatus(Status.Connected, "Conectado. Autenticando…");
        }

        private void Update()
        {
            if (_socket == null)
                return;

            while (_socket.TryDequeue(out var message))
                Handle(message);

            if (IsHumanTurn)
                TurnTimeLeft = Mathf.Max(0f, TurnTimeLeft - Time.deltaTime);
        }

        private void Handle(ServerMessage message)
        {
            switch (message)
            {
                case Welcome w:
                    PlayerId = w.PlayerId;
                    SetStatus(View == null ? Status.Connected : Status.InMatch, $"Olá, {w.Name} (nível {w.Level}).");
                    Send(new ProfileRequest());
                    break;
                case Queued q:
                    Waiting = q.Waiting;
                    SetStatus(Status.Queued, $"Na fila ({q.Mission}): {q.Waiting} esperando. IA completa em {q.SecondsUntilAiFill} s.");
                    break;
                case QueueLeft:
                    SetStatus(Status.Connected, "Saiu da fila.");
                    break;
                case MatchStarted ms:
                    RoomId = ms.RoomId;
                    HumanId = ms.HunterId;
                    ApplySnapshot(ms.Snapshot);
                    SetStatus(Status.InMatch, "Partida encontrada!");
                    break;
                case MatchResumed mr:
                    RoomId = mr.RoomId;
                    HumanId = mr.HunterId;
                    ApplySnapshot(mr.Snapshot);
                    SetStatus(Status.InMatch, "Reconectado.");
                    break;
                case MatchUpdate mu:
                    ApplySnapshot(mu.Snapshot);
                    foreach (var e in mu.Events)
                        OnEvent?.Invoke(e);
                    OnStateChanged?.Invoke();
                    break;
                case GameOver go:
                    ApplySnapshot(go.Snapshot);
                    OnStateChanged?.Invoke();
                    SetStatus(Status.Finished, "Partida encerrada.");
                    var reward = GameSession.RewardFor(GameSession.Mission);
                    GameSession.LastOutcome = View.ToOutcome(Content, reward.Gold, reward.Xp);
                    GameSession.LastReward = null;
                    OnFinished?.Invoke(GameSession.LastOutcome);
                    break;
                case MatchRewarded rewarded:
                    GameSession.LastReward = rewarded.Summary;
                    OnGuildMessage?.Invoke(message);
                    break;
                case ProfileState or ShopState or Ranking:
                    OnGuildMessage?.Invoke(message);
                    break;
                case Rejected r:
                    OnEvent?.Invoke(new ActionRejected(new Pass(HumanId), r.Reason));
                    break;
                case Error err:
                    if (err.Message == "desconectado")
                    {
                        if (_wantReconnect)
                        {
                            SetStatus(Status.Disconnected, "Conexão perdida. Reconectando…");
                            StartCoroutine(ReconnectLater());
                        }
                    }
                    else
                    {
                        OnGuildMessage?.Invoke(message);
                        if (View != null)
                            OnEvent?.Invoke(new ActionRejected(new Pass(HumanId), err.Message));
                        else
                            SetStatus(State, err.Message);
                    }
                    break;
            }
        }

        private IEnumerator ReconnectLater()
        {
            yield return new WaitForSeconds(ReconnectSeconds);
            if (_wantReconnect)
                StartCoroutine(ConnectRoutine());
        }

        private void ApplySnapshot(PlayerSnapshot snapshot)
        {
            View = snapshot;
            var key = snapshot.Round * 16 + (snapshot.CurrentHunterId ?? 0);
            if (key != _lastTurnKey || snapshot.Phase == GamePhase.AwaitingRoll)
            {
                _lastTurnKey = key;
                TurnTimeLeft = 45f;
            }
        }

        private void SetStatus(Status status, string text)
        {
            State = status;
            StatusText = text;
            OnStatusChanged?.Invoke();
        }

        private void OnDestroy() => _socket?.Dispose();
    }
}
