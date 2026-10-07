using System.Collections;
using System.Collections.Generic;
using BattleHunter.Client.Offline;
using BattleHunter.Client.Presentation;
using BattleHunter.Client.UI;
using BattleHunter.Core.State.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Cena Partida: host offline + tabuleiro + HUD + mão + toque. Eventos são animados em fila, na ordem.</summary>
    public sealed class MatchScreen : MonoBehaviour
    {
        public OfflineGameHost Host { get; private set; }

        private BoardView _board;
        private readonly Queue<GameEvent> _animations = new();
        private bool _built;

        private void Start()
        {
            Host = gameObject.AddComponent<OfflineGameHost>();
            Host.OnStateChanged += OnStateChanged;
            Host.OnEvent += e => _animations.Enqueue(e);
            Host.OnFinished += _ => StartCoroutine(GoToResult());
            StartCoroutine(AnimationPump());
        }

        private void OnStateChanged()
        {
            if (_built)
            {
                _board.Refresh(Host.State);
                return;
            }

            _built = true;
            var canvas = Ui.Canvas("MatchCanvas").transform;
            _board = new GameObject("BoardView").AddComponent<BoardView>();
            _board.Build(Host.State, Host.Content, Host.HumanId);

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
                    _board.Refresh(Host.State);
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
