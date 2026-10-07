using System.Collections;
using System.Collections.Generic;
using BattleHunter.Client.Net;
using BattleHunter.Client.Offline;
using BattleHunter.Client.Presentation;
using BattleHunter.Client.UI;
using BattleHunter.Core.State.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Cena Partida: host (offline ou online) + tabuleiro + HUD + mão + toque. Eventos são animados em fila, na ordem.</summary>
    public sealed class MatchScreen : MonoBehaviour
    {
        public IGameHost Host { get; private set; }

        private BoardView _board;
        private readonly Queue<GameEvent> _animations = new();
        private bool _built;

        private void Start()
        {
            if (GameSession.Mode == GameMode.Online)
            {
                var online = FindFirstObjectByType<OnlineGameHost>();
                if (online == null)
                {
                    SceneManager.LoadScene("Lobby");
                    return;
                }
                Host = online;
            }
            else
            {
                Host = gameObject.AddComponent<OfflineGameHost>();
            }

            var audio = gameObject.AddComponent<ProceduralAudio>();
            Host.OnStateChanged += OnStateChanged;
            Host.OnEvent += e =>
            {
                _animations.Enqueue(e);
                if (GameSession.AiDelay > 0f)
                    audio.Play(e);
            };
            Host.OnFinished += _ => StartCoroutine(GoToResult());
            StartCoroutine(AnimationPump());
            if (Host.View != null)
                OnStateChanged();
        }

        private void OnStateChanged()
        {
            if (Host.View == null)
                return;

            if (_built)
            {
                _board.Refresh(Host.View);
                return;
            }

            _built = true;
            var canvas = Ui.Canvas("MatchCanvas").transform;
            _board = new GameObject("BoardView").AddComponent<BoardView>();
            _board.Build(Host.View);

            var hud = gameObject.AddComponent<HudView>();
            hud.Build(canvas, Host);
            var hand = gameObject.AddComponent<HandView>();
            hand.Build(canvas, Host);
            var input = gameObject.AddComponent<InputController>();
            input.Bind(Host, _board, hand, hud);
        }

        private IEnumerator AnimationPump()
        {
            while (true)
            {
                if (_animations.Count == 0 || _board == null)
                {
                    yield return null;
                    continue;
                }

                yield return _board.Animate(_animations.Dequeue());
                if (_animations.Count == 0)
                    _board.Refresh(Host.View);
            }
        }

        private IEnumerator GoToResult()
        {
            var wait = GameSession.AiDelay > 0f ? 3f : 0f;
            yield return new WaitForSeconds(wait);
            SceneManager.LoadScene("Resultado");
        }
    }
}
