using BattleHunter.Client.Progression;
using BattleHunter.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Tela inicial: guilda (perfil local, progressão), partida rápida offline, online.</summary>
    public sealed class BootScreen : MonoBehaviour
    {
        private void Start()
        {
            GameSession.Mode = GameMode.Offline;
            GameSession.ProfileService = null;
            var canvas = Ui.Canvas("BootCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, Ui.Background, framed: false);
            Ui.Label(canvas, "Title", "BATTLE HUNTER", 96, TextAnchor.MiddleCenter, new Vector2(0f, 0.72f), new Vector2(1f, 0.9f), color: Ui.Accent);
            Ui.Label(canvas, "Sub", "Caça ao tesouro, 4 caçadores, um dungeon.", 32, TextAnchor.MiddleCenter, new Vector2(0f, 0.64f), new Vector2(1f, 0.72f));

            Ui.Button(canvas, "Guild", "Guilda — seu caçador, missões, loja", new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.58f), () =>
            {
                GameSession.ProfileService = new LocalProfileService(ContentLoader.Load());
                SceneManager.LoadScene("Guilda");
            }, fontSize: 32, color: new Color(0.45f, 0.35f, 0.15f));

            Quick(canvas, "Partida rápida (fácil)", "easy", 0.4f);
            Quick(canvas, "Partida rápida (normal, chefe)", "normal", 0.31f);

            Ui.Button(canvas, "Lobby", "Jogar online", new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.27f), () => SceneManager.LoadScene("Lobby"), fontSize: 30, color: new Color(0.2f, 0.4f, 0.55f));
            Ui.Label(canvas, "Version", $"offline · seed {GameSession.Seed}", 22, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0.06f), color: new Color(0.6f, 0.6f, 0.6f));
        }

        private static void Quick(Transform canvas, string caption, string mission, float y)
        {
            Ui.Button(canvas, "Play " + mission, caption, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.08f), () =>
            {
                GameSession.Mode = GameMode.Offline;
                GameSession.ProfileService = null;
                GameSession.Mission = mission;
                GameSession.NewSeed();
                SceneManager.LoadScene("Partida");
            }, fontSize: 30);
        }
    }
}
