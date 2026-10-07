using System.Linq;
using BattleHunter.Client.Net;
using BattleHunter.Client.Progression;
using BattleHunter.Client.UI;
using BattleHunter.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Screens
{
    /// <summary>Tela de resultado: vencedor, cartas mantidas, ouro e XP; com perfil, mostra a recompensa aplicada e a subida de nível.</summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        private void Start()
        {
            var o = GameSession.LastOutcome;
            var r = GameSession.LastReward;
            var canvas = Ui.Canvas("ResultCanvas").transform;
            Ui.PanelRect(canvas, "Bg", Vector2.zero, Vector2.one, Ui.Background);

            var online = FindFirstObjectByType<OnlineGameHost>();
            var isOnline = GameSession.Mode == GameMode.Online && online != null;
            var hasGuild = GameSession.ProfileService != null && (isOnline || GameSession.ProfileService is LocalProfileService { Profile: not null });

            var won = r?.Won ?? (o != null && o.MissionGold > 0);
            var title = o == null ? "Sem partida" : o.Reason switch
            {
                GameEndReason.TreasureExtracted when won => "Missão cumprida!",
                GameEndReason.TreasureExtracted => $"{o.WinnerName} levou o tesouro",
                GameEndReason.RoundLimit => "Limite de rodadas",
                _ => "Todos caíram",
            };
            Ui.Label(canvas, "Title", title, 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.8f), new Vector2(1f, 0.93f), color: Ui.Accent);

            if (o != null)
            {
                var cards = ContentLoader.Load().Cards;
                var status = o.HumanStatus switch
                {
                    HunterStatus.Exited => "saiu do dungeon",
                    HunterStatus.Fallen => "caiu",
                    _ => "ficou no dungeon",
                };
                string body;
                if (r != null)
                {
                    var kept = r.CardsKept.Count == 0 ? "nenhuma" : string.Join(", ", r.CardsKept.Select(id => cards.Get(id).Name));
                    body =
                        $"Rodadas: {o.Rounds}. Você {status}.\n\n" +
                        $"XP: +{r.XpGained}" + (r.LevelsGained > 0 ? $"  —  subiu {r.LevelsGained} nível(is)! Gaste o ponto livre na guilda." : "") + "\n" +
                        $"Ouro: +{r.GoldFound} (baús) +{r.GoldFromMission} (missão) +{r.GoldFromSales} (cartas vendidas)" + (r.GoldLost > 0 ? $"  −{r.GoldLost} (queda)" : "") + "\n" +
                        $"Cartas mantidas ({r.CardsKept.Count}/5): {kept}\n" +
                        (r.CardsSold.Count > 0 ? $"Vendidas: {string.Join(", ", r.CardsSold.Select(id => cards.Get(id).Name))}\n" : "") +
                        (r.RareCard != null ? $"Carta rara: {cards.Get(r.RareCard).Name}\n" : "") +
                        (r.RankDelta != 0 ? $"Rank: {r.RankDelta:+#;-#}\n" : "");
                }
                else
                {
                    var kept = o.CardsKept.Count == 0 ? "nenhuma" : string.Join(", ", o.CardsKept.Select(id => cards.Get(id).Name));
                    body =
                        $"Rodadas: {o.Rounds}. Você {status}.\n\n" +
                        $"Cartas mantidas ({o.CardsKept.Count}/{GameSession.MaxCardsKept}): {kept}\n" +
                        $"Ouro: {o.GoldFromCards} (cartas vendidas) + {o.MissionGold} (missão)\n" +
                        $"XP: {o.Xp} + {o.MissionXp} (missão)\n\n<size=24>Partida rápida: sem perfil, nada é guardado.</size>";
                }

                var label = Ui.Label(canvas, "Body", body, 30, TextAnchor.UpperLeft, new Vector2(0.07f, 0.38f), new Vector2(0.93f, 0.78f));
                label.supportRichText = true;
            }

            if (hasGuild)
            {
                Ui.Button(canvas, "Guild", "Voltar à guilda", new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.34f), () =>
                {
                    if (isOnline)
                        online.ResetMatch();
                    SceneManager.LoadScene("Guilda");
                });
            }
            else
            {
                Ui.Button(canvas, "Again", isOnline ? "Nova partida online" : "Jogar de novo", new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.34f), () =>
                {
                    if (isOnline)
                    {
                        online.ResetMatch();
                        SceneManager.LoadScene("Lobby");
                        return;
                    }

                    GameSession.NewSeed();
                    SceneManager.LoadScene("Partida");
                });
            }

            Ui.Button(canvas, "Home", "Início", new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.24f), () =>
            {
                if (online != null)
                    online.Shutdown();
                GameSession.ProfileService = null;
                GameSession.Mode = GameMode.Offline;
                SceneManager.LoadScene("Boot");
            });
        }
    }
}
