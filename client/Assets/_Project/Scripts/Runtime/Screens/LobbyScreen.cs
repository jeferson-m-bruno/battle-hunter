using BattleHunter.Client.Net;
using BattleHunter.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleHunter.Client.Screens
{
    /// <summary>Lobby online: servidor, nome, missão → fila → sala de espera → Partida. Reconexão é do host.</summary>
    public sealed class LobbyScreen : MonoBehaviour
    {
        private OnlineGameHost _host;
        private Text _status;
        private InputField _url;
        private InputField _name;
        private RectTransform _missions;
        private Button _cancel;

        private void Start()
        {
            var canvas = Ui.Canvas("LobbyCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, new Color(0.08f, 0.07f, 0.10f));
            Ui.Label(canvas, "Title", "Online", 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.86f), new Vector2(1f, 0.96f), color: Ui.Accent);

            Ui.Label(canvas, "UrlLabel", "Servidor", 26, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.79f), new Vector2(0.9f, 0.83f));
            _url = Field(canvas, "Url", GameSession.ServerUrl, 0.72f);
            Ui.Label(canvas, "NameLabel", "Nome do caçador", 26, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.69f));
            _name = Field(canvas, "Name", GameSession.HunterName, 0.58f);

            _missions = Ui.PanelRect(canvas, "Missions", new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.54f), Color.clear);
            Mission("Caça (fácil)", "easy", 0.76f);
            Mission("Caça (normal)", "normal", 0.52f);
            Mission("Caça (difícil)", "hard", 0.28f);
            Mission("Ranqueada", "ranked", 0.04f);

            _status = Ui.Label(canvas, "Status", "Escolha uma missão para entrar na fila.", 26, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.25f), color: Ui.Accent);
            _cancel = Ui.Button(canvas, "Cancel", "Sair da fila", new Vector2(0.1f, 0.09f), new Vector2(0.48f, 0.16f), () => _host?.LeaveQueue(), fontSize: 28);
            _cancel.gameObject.SetActive(false);
            Ui.Button(canvas, "Back", "Voltar", new Vector2(0.52f, 0.09f), new Vector2(0.9f, 0.16f), () =>
            {
                _host?.Shutdown();
                SceneManager.LoadScene("Boot");
            }, fontSize: 28);

            // Um host online sobrevivente (partida encerrada) é descartado.
            var leftover = FindFirstObjectByType<OnlineGameHost>();
            if (leftover != null)
                leftover.Shutdown();
        }

        private InputField Field(Transform canvas, string name, string value, float y)
        {
            var rt = Ui.PanelRect(canvas, name, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.065f), new Color(0.2f, 0.19f, 0.25f));
            var field = rt.gameObject.AddComponent<InputField>();
            var text = Ui.Label(rt, "Text", value, 28, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
            text.raycastTarget = true;
            text.supportRichText = false;
            field.textComponent = text;
            field.text = value;
            return field;
        }

        private void Mission(string caption, string id, float y)
        {
            Ui.Button(_missions, "Mission " + id, caption, new Vector2(0f, y), new Vector2(1f, y + 0.2f), () => Join(id), fontSize: 30);
        }

        private void Join(string mission)
        {
            GameSession.ServerUrl = string.IsNullOrWhiteSpace(_url.text) ? GameSession.ServerUrl : _url.text.Trim();
            GameSession.HunterName = string.IsNullOrWhiteSpace(_name.text) ? "Caçador" : _name.text.Trim();
            GameSession.Mission = mission;
            GameSession.Mode = GameMode.Online;

            if (_host == null)
            {
                _host = new GameObject("OnlineGameHost").AddComponent<OnlineGameHost>();
                _host.OnStatusChanged += OnStatus;
            }

            _host.Connect(GameSession.ServerUrl, mission);
            _missions.gameObject.SetActive(false);
            _cancel.gameObject.SetActive(true);
        }

        private void OnStatus()
        {
            if (_host == null)
                return;

            _status.text = _host.StatusText;
            if (_host.State == OnlineGameHost.Status.InMatch)
            {
                _host.OnStatusChanged -= OnStatus;
                SceneManager.LoadScene("Partida");
            }
            else if (_host.State == OnlineGameHost.Status.Connected)
            {
                _missions.gameObject.SetActive(true);
                _cancel.gameObject.SetActive(false);
            }
        }
    }
}
