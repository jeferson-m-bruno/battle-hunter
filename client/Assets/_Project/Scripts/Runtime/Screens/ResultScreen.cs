using System.Linq;
using BattleHunter.Client.Net;
using BattleHunter.Client.UI;
using BattleHunter.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Tela de resultado: vencedor, cartas mantidas (até 5), ouro e XP; Jogar de novo (offline: nova seed; online: Lobby).</summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        private void Start()
        {
            var o = GameSession.LastOutcome;
            var canvas = Ui.Canvas("ResultCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, new Color(0.08f, 0.07f, 0.10f));

            var online = FindFirstObjectByType<OnlineGameHost>();
            if (online != null)
                online.Shutdown();

            var title = o == null ? "Sem partida" : o.Reason switch
            {
                GameEndReason.TreasureExtracted when o.WinnerId == MyId(o) => "Missão cumprida!",
                GameEndReason.TreasureExtracted => $"{o.WinnerName} levou o tesouro",
                GameEndReason.RoundLimit => "Limite de rodadas",
                _ => "Todos caíram",
            };
            Ui.Label(canvas, "Title", title, 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.78f), new Vector2(1f, 0.92f), color: Ui.Accent);

            if (o != null)
            {
                var cards = ContentLoader.Load().Cards;
                var kept = o.CardsKept.Count == 0 ? "nenhuma" : string.Join(", ", o.CardsKept.Select(id => cards.Get(id).Name));
                var status = o.HumanStatus switch
                {
                    HunterStatus.Exited => "saiu do dungeon",
                    HunterStatus.Fallen => "caiu (perde 10% do ouro não depositado)",
                    _ => "ficou no dungeon",
                };
                var body =
                    $"Rodadas: {o.Rounds}\n" +
                    $"Você {status}.\n\n" +
                    $"Cartas mantidas ({o.CardsKept.Count}/{GameSession.MaxCardsKept}): {kept}\n" +
                    $"Ouro: {o.GoldFromCards} (cartas vendidas) + {o.MissionGold} (missão)\n" +
                    $"XP: {o.Xp} + {o.MissionXp} (missão)";
                Ui.Label(canvas, "Body", body, 32, TextAnchor.UpperLeft, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.76f));
            }

            var again = GameSession.Mode == GameMode.Online ? "Nova partida online" : "Jogar de novo";
            Ui.Button(canvas, "Again", again, new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.34f), () =>
            {
                if (GameSession.Mode == GameMode.Online)
                {
                    SceneManager.LoadScene("Lobby");
                    return;
                }

                GameSession.NewSeed();
                SceneManager.LoadScene("Partida");
            });
            Ui.Button(canvas, "Home", "Início", new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.24f), () =>
            {
                GameSession.Mode = GameMode.Offline;
                SceneManager.LoadScene("Boot");
            });
        }

        /// <summary>No offline o humano é sempre 1; no online o id vem da sala e o resultado já foi calculado com ele.</summary>
        private static int MyId(MatchOutcome o) => o.WinnerId != null && o.HumanStatus == HunterStatus.Exited && o.MissionGold > 0 ? o.WinnerId.Value : -1;
    }
}
