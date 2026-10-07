using BattleHunter.Client.UI;
using BattleHunter.Core.Ai;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Tela inicial: jogar offline (fácil/normal/difícil), online (lobby) e guilda (stub).</summary>
    public sealed class BootScreen : MonoBehaviour
    {
        private void Start()
        {
            GameSession.Mode = GameMode.Offline;
            var canvas = Ui.Canvas("BootCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, new Color(0.08f, 0.07f, 0.10f));
            Ui.Label(canvas, "Title", "BATTLE HUNTER", 96, TextAnchor.MiddleCenter, new Vector2(0f, 0.72f), new Vector2(1f, 0.9f), color: Ui.Accent);
            Ui.Label(canvas, "Sub", "Caça ao tesouro, 4 caçadores, um dungeon.", 32, TextAnchor.MiddleCenter, new Vector2(0f, 0.64f), new Vector2(1f, 0.72f));

            Play(canvas, "Caça (fácil) — 12×12, sem chefe", MatchSettings.Easy, "easy", 0.52f);
            Play(canvas, "Caça (normal) — 14×14, chefe", MatchSettings.Normal, "normal", 0.43f);
            Play(canvas, "Caça (difícil) — 16×16, chefe ×1.5", MatchSettings.Hard, "hard", 0.34f);

            Ui.Button(canvas, "Lobby", "Jogar online", new Vector2(0.1f, 0.2f), new Vector2(0.48f, 0.27f), () => SceneManager.LoadScene("Lobby"), fontSize: 28, color: new Color(0.2f, 0.4f, 0.55f));
            Ui.Button(canvas, "Guild", "Guilda (em breve)", new Vector2(0.52f, 0.2f), new Vector2(0.9f, 0.27f), () => SceneManager.LoadScene("Guilda"), fontSize: 28);
            Ui.Label(canvas, "Version", $"offline · seed {GameSession.Seed}", 22, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0.06f), color: new Color(0.6f, 0.6f, 0.6f));
        }

        private static void Play(Transform canvas, string caption, MatchSettings settings, string mission, float y)
        {
            Ui.Button(canvas, "Play " + caption, caption, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.08f), () =>
            {
                GameSession.Mode = GameMode.Offline;
                GameSession.Settings = settings;
                GameSession.Mission = mission;
                GameSession.NewSeed();
                SceneManager.LoadScene("Partida");
            }, fontSize: 32);
        }
    }
}
