using BattleHunter.Client.Net;
using BattleHunter.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleHunter.Client.Screens
{
    /// <summary>Lobby online: conecta, abre a guilda online (perfil/loja/loadout/ranking) ou entra na fila; sala de espera; Partida.</summary>
    public sealed class LobbyScreen : MonoBehaviour
    {
        private OnlineGameHost _host;
        private Text _status;
        private InputField _url;
        private InputField _name;
        private RectTransform _actions;
        private Button _cancel;
        private Button _guild;

        private void Start()
        {
            var canvas = Ui.Canvas("LobbyCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, Ui.Background, framed: false);
            Ui.Label(canvas, "Title", "Online", 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.86f), new Vector2(1f, 0.96f), color: Ui.Accent);

            Ui.Label(canvas, "UrlLabel", "Servidor", 26, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.79f), new Vector2(0.9f, 0.83f));
            _url = Ui.Field(canvas, "Url", GameSession.ServerUrl, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.785f), maxLength: 120);
            Ui.Label(canvas, "NameLabel", "Nome do caçador", 26, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.69f));
            _name = Ui.Field(canvas, "Name", GameSession.HunterName, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.645f));

            _actions = Ui.PanelRect(canvas, "Actions", new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.54f), Color.clear);
            Ui.Button(_actions, "Connect", "Conectar", new Vector2(0f, 0.76f), new Vector2(1f, 0.96f), Connect, fontSize: 30, color: new Color(0.2f, 0.4f, 0.55f));
            _guild = Ui.Button(_actions, "Guild", "Guilda online (missões, loja, loadout)", new Vector2(0f, 0.52f), new Vector2(1f, 0.72f), () =>
            {
                if (_host == null || _host.Guild?.Profile == null)
                {
                    Show("Conecte primeiro.");
                    return;
                }

                GameSession.ProfileService = _host.Guild;
                SceneManager.LoadScene("Guilda");
            }, fontSize: 28);
            Ui.Button(_actions, "Quick", "Partida rápida (fácil, sem guilda)", new Vector2(0f, 0.28f), new Vector2(1f, 0.48f), () => Join("easy"), fontSize: 28);

            _status = Ui.Label(canvas, "Status", "Conecte para entrar na guilda ou numa partida.", 26, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.25f), color: Ui.Accent);
            _cancel = Ui.Button(canvas, "Cancel", "Sair da fila", new Vector2(0.1f, 0.09f), new Vector2(0.48f, 0.16f), () => _host?.LeaveQueue(), fontSize: 28);
            _cancel.gameObject.SetActive(false);
            Ui.Button(canvas, "Back", "Voltar", new Vector2(0.52f, 0.09f), new Vector2(0.9f, 0.16f), () =>
            {
                _host?.Shutdown();
                GameSession.ProfileService = null;
                GameSession.Mode = GameMode.Offline;
                SceneManager.LoadScene("Boot");
            }, fontSize: 28);

            // Host sobrevivente (vindo da guilda online ou de uma partida encerrada).
            _host = FindFirstObjectByType<OnlineGameHost>();
            if (_host != null)
            {
                _host.OnStatusChanged += OnStatus;
                if (_host.State == OnlineGameHost.Status.Finished)
                    _host.ResetMatch();
                OnStatus();
            }
        }

        private void Connect()
        {
            GameSession.ServerUrl = string.IsNullOrWhiteSpace(_url.text) ? GameSession.ServerUrl : _url.text.Trim();
            GameSession.HunterName = string.IsNullOrWhiteSpace(_name.text) ? "Caçador" : _name.text.Trim();
            GameSession.Mode = GameMode.Online;

            if (_host == null)
            {
                _host = new GameObject("OnlineGameHost").AddComponent<OnlineGameHost>();
                _host.OnStatusChanged += OnStatus;
            }

            _host.Connect(GameSession.ServerUrl);
        }

        private void Join(string mission)
        {
            if (_host == null || _host.State is OnlineGameHost.Status.Disconnected or OnlineGameHost.Status.Connecting)
            {
                Connect();
                StartCoroutine(JoinWhenConnected(mission));
                return;
            }

            GameSession.Mode = GameMode.Online;
            _host.JoinQueue(mission);
        }

        private System.Collections.IEnumerator JoinWhenConnected(string mission)
        {
            var deadline = Time.time + 15f;
            while (Time.time < deadline && (_host == null || _host.State != OnlineGameHost.Status.Connected))
                yield return null;
            if (_host != null && _host.State == OnlineGameHost.Status.Connected)
                _host.JoinQueue(mission);
        }

        private void Show(string text) => _status.text = text;

        private void OnStatus()
        {
            if (_host == null)
                return;

            _status.text = _host.StatusText;
            var queued = _host.State == OnlineGameHost.Status.Queued;
            _actions.gameObject.SetActive(!queued);
            _cancel.gameObject.SetActive(queued);

            if (_host.State == OnlineGameHost.Status.InMatch)
            {
                _host.OnStatusChanged -= OnStatus;
                GameSession.ProfileService = _host.Guild;
                SceneManager.LoadScene("Partida");
            }
        }

        private void OnDestroy()
        {
            if (_host != null)
                _host.OnStatusChanged -= OnStatus;
        }
    }
}
