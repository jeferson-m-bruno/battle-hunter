using System.Collections;
using System.IO;
using System.Linq;
using BattleHunter.Client;
using BattleHunter.Client.Progression;
using BattleHunter.Client.Screens;
using BattleHunter.Core.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleHunter.Client.Tests
{
    /// <summary>Gate da fatia 7 (offline): guilda local cria o caçador, a partida aplica a recompensa no perfil gravado em disco.</summary>
    public class LocalGuildTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Application.temporaryCachePath, "test-profile-" + System.Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path))
                File.Delete(_path);
            GameSession.ProfileService = null;
            GameSession.AutoPilot = false;
        }

        [UnityTest]
        public IEnumerator Given_LocalProfile_When_MatchPlayed_Then_RewardAppliedAndSaved()
        {
            var content = ContentLoader.Load();
            var service = new LocalProfileService(content, _path);
            Assert.IsFalse(service.HasProfile);
            service.SetAppearance("Teste", 1, 2);
            Assert.IsTrue(File.Exists(_path));
            Assert.AreEqual(("Teste", 1, 2, 1), (service.Profile.Name, service.Profile.ColorIndex, service.Profile.FaceIndex, service.Profile.Level));

            GameSession.ProfileService = service;
            GameSession.AutoPilot = true;
            GameSession.AiDelay = 0f;
            GameSession.Mission = "easy";
            GameSession.Mode = GameMode.Offline;
            GameSession.Seed = 7;
            GameSession.LastOutcome = null;
            GameSession.LastReward = null;

            yield return SceneManager.LoadSceneAsync("Partida");
            var deadline = Time.realtimeSinceStartup + 120f;
            while (GameSession.LastOutcome == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsNotNull(GameSession.LastOutcome, "a partida não terminou");
            Assert.IsNotNull(GameSession.LastReward, "a recompensa não foi aplicada ao perfil local");
            var reloaded = new LocalProfileService(content, _path);
            Assert.AreEqual(service.Profile.Xp, reloaded.Profile.Xp);
            Assert.AreEqual(GameSession.LastReward.XpGained, reloaded.Profile.Xp);
            Assert.AreEqual(GameSession.LastReward.CardsKept.Count + (GameSession.LastReward.RareCard != null ? 1 : 0), reloaded.Profile.Inventory.Count);

            while (SceneManager.GetActiveScene().name != "Resultado" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<ResultScreen>());
        }

        [UnityTest]
        public IEnumerator Given_LocalProfile_When_ShopUsed_Then_BuySellDepositFollowCoreRules()
        {
            var content = ContentLoader.Load();
            var service = new LocalProfileService(content, _path);
            service.SetAppearance("Rico", 0, 0);
            // Ouro de teste: grava um perfil com 1000 de ouro e recarrega.
            File.WriteAllText(_path, Core.Serialization.MessageJson.Serialize(service.Profile with { Gold = 1000 }));
            service = new LocalProfileService(content, _path);

            var stock = service.ShopStock;
            Assert.AreEqual(9, stock.Count);
            var card = content.Cards.Get(stock[0]);
            service.Buy(card.Id);
            Assert.AreEqual(1000 - Shop.PriceOf(card), service.Profile.Gold);
            Assert.Contains(card.Id, service.Profile.Inventory.ToList());

            service.Sell(card.Id);
            Assert.AreEqual(1000 - Shop.PriceOf(card) + card.Sell, service.Profile.Gold);

            service.Deposit(100);
            Assert.AreEqual(100, service.Profile.BankedGold);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_GuildScene_When_NoProfile_Then_ShowsHunterCreation()
        {
            GameSession.ProfileService = new LocalProfileService(ContentLoader.Load(), _path);
            yield return SceneManager.LoadSceneAsync("Guilda");
            yield return null;

            Assert.IsNotNull(Object.FindFirstObjectByType<GuildScreen>());
            Assert.IsNotNull(GameObject.Find("Create"), "botão de criação do caçador não apareceu");
        }
    }
}
