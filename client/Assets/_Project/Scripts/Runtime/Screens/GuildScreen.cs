using BattleHunter.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Stub da Guilda (fatia 7): escolher missão, equipar, vender cartas, ranking.</summary>
    public sealed class GuildScreen : MonoBehaviour
    {
        private void Start()
        {
            var canvas = Ui.Canvas("GuildCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, new Color(0.08f, 0.07f, 0.10f));
            Ui.Label(canvas, "Title", "Guilda", 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.7f), new Vector2(1f, 0.85f), color: Ui.Accent);
            Ui.Label(canvas, "Body", "Níveis, loja, tipos de missão e ranking chegam na fatia 7.", 30, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.7f));
            Ui.Button(canvas, "Back", "Voltar", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.38f), () => SceneManager.LoadScene("Boot"));
        }
    }
}
