using System.Collections;
using System.Net.Sockets;
using BattleHunter.Client;
using BattleHunter.Client.Net;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BattleHunter.Client.Tests
{
    /// <summary>
    /// Gate da fatia 6 no cliente: OnlineGameHost conecta num servidor local, entra na fila, a IA completa a sala
    /// e a partida vai até GameOver. Ignorado quando não há servidor em localhost:5000
    /// (suba com BattleHunter__QueueFillSeconds=0 BattleHunter__AiDelayMs=0 dotnet run --project server).
    /// </summary>
    public class OnlineMatchTests
    {
        private const string Url = "ws://localhost:5000/ws";

        [UnityTest]
        public IEnumerator Given_LocalServer_When_HostQueues_Then_MatchStartsAndReachesGameOver()
        {
            if (!ServerIsUp())
            {
                Assert.Ignore("Servidor local não está em localhost:5000; teste online ignorado.");
                yield break;
            }

            GameSession.HunterName = "Teste";
            GameSession.Mission = "easy";
            var host = new GameObject("OnlineGameHost").AddComponent<OnlineGameHost>();
            MatchOutcome outcome = null;
            host.OnFinished += o => outcome = o;
            host.Connect(Url);

            var deadline = Time.realtimeSinceStartup + 90f;
            while (host.State != OnlineGameHost.Status.Connected && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(OnlineGameHost.Status.Connected, host.State, $"não conectou: {host.StatusText}");

            host.JoinQueue("easy");
            while (host.View == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsNotNull(host.View, $"MatchStarted não chegou: {host.StatusText}");
            Assert.AreEqual(OnlineGameHost.Status.InMatch, host.State);
            Assert.AreEqual(4, host.View.Hunters.Count);
            Assert.AreEqual(host.HumanId, host.View.Self.Id);

            var lastKey = -1;
            while (outcome == null && Time.realtimeSinceStartup < deadline)
            {
                if (host.IsHumanTurn)
                {
                    var v = host.View;
                    var key = v.Round * 100 + (v.Phase == GamePhase.AwaitingRoll ? 0 : 1);
                    if (key != lastKey)
                    {
                        lastKey = key;
                        host.Submit(v.Phase == GamePhase.AwaitingRoll ? new RollDice(host.HumanId) : new Pass(host.HumanId));
                    }
                }

                yield return null;
            }

            Assert.IsNotNull(outcome, $"GameOver não chegou: {host.StatusText}");
            Assert.That(outcome.Rounds, Is.InRange(1, 30));
            Assert.AreEqual(OnlineGameHost.Status.Finished, host.State);
            host.Shutdown();
        }

        private static bool ServerIsUp()
        {
            try
            {
                using var tcp = new TcpClient();
                var task = tcp.ConnectAsync("127.0.0.1", 5000);
                return task.Wait(1500) && tcp.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
