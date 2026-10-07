using BattleHunter.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Stub do Lobby (fatia 6): fila por missão, sala de espera, reconexão.</summary>
    public sealed class LobbyScreen : MonoBehaviour
    {
        private void Start()
        {
            var canvas = Ui.Canvas("LobbyCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, new Color(0.08f, 0.07f, 0.10f));
            Ui.Label(canvas, "Title", "Online", 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.7f), new Vector2(1f, 0.85f), color: Ui.Accent);
            Ui.Label(canvas, "Body", "Matchmaking e partidas online chegam na fatia 6.", 30, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.7f));
            Ui.Button(canvas, "Back", "Voltar", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.38f), () => SceneManager.LoadScene("Boot"));
        }
    }
}
