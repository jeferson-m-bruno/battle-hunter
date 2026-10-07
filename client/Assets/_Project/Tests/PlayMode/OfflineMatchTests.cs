using System.Collections;
using BattleHunter.Client;
using BattleHunter.Client.Screens;
using BattleHunter.Core.Ai;
using BattleHunter.Core.State;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleHunter.Client.Tests
{
    /// <summary>Gate da fatia 5 em batchmode: a cena Partida roda uma partida inteira (humano em piloto automático + 3 IAs) até o resultado.</summary>
    public class OfflineMatchTests
    {
        [UnityTest]
        public IEnumerator Given_PartidaScene_When_AutoPilot_Then_MatchFinishesAndResultIsShown()
        {
            GameSession.AutoPilot = true;
            GameSession.AiDelay = 0f;
            GameSession.Settings = MatchSettings.Easy;
            GameSession.Seed = 42;
            GameSession.LastOutcome = null;

            yield return SceneManager.LoadSceneAsync("Partida");
            yield return null;

            var screen = Object.FindFirstObjectByType<MatchScreen>();
            Assert.IsNotNull(screen, "MatchScreen não encontrada na cena Partida");

            var deadline = Time.realtimeSinceStartup + 120f;
            while (GameSession.LastOutcome == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsNotNull(GameSession.LastOutcome, "a partida não terminou em 120 s");
            Assert.That(GameSession.LastOutcome.Rounds, Is.InRange(1, 30));

            while (SceneManager.GetActiveScene().name != "Resultado" && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.AreEqual("Resultado", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindFirstObjectByType<ResultScreen>());
        }

        [UnityTest]
        public IEnumerator Given_ContentInResources_When_Loaded_Then_HasAllCardsAndMonsters()
        {
            var content = ContentLoader.Load();

            Assert.AreEqual(55, content.Cards.All.Count);
            Assert.AreEqual(6, content.Monsters.All.Count);
            Assert.IsTrue(content.LootTables.ContainsKey("chest"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_BootScene_When_Loaded_Then_HasPlayButtons()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return null;

            Assert.IsNotNull(GameObject.Find("BootCanvas"));
            Assert.IsNotNull(Object.FindFirstObjectByType<BootScreen>());
        }
    }
}
