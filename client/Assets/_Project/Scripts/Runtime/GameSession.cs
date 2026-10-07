using System.Collections.Generic;
using BattleHunter.Client.Progression;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Progression;
using BattleHunter.Core.State;
using UnityEngine;

namespace BattleHunter.Client
{
    public enum GameMode
    {
        Offline,
        Online,
    }

    /// <summary>Resumo da partida para a tela de resultado.</summary>
    public sealed class MatchOutcome
    {
        public GameEndReason Reason;
        public int? WinnerId;
        public string WinnerName;
        public int Rounds;
        public HunterStatus HumanStatus;
        public IReadOnlyList<string> CardsKept;
        public int GoldFromCards;
        public int MissionGold;
        public int Xp;
        public int MissionXp;
    }

    /// <summary>Estado que atravessa cenas: modo, missão, perfil em uso, seed da próxima partida e o último resultado.</summary>
    public static class GameSession
    {
        public const int HumanHunterId = 1;
        public const int MaxCardsKept = 5;

        public static GameMode Mode = GameMode.Offline;
        public static MatchSettings Settings = MatchSettings.Easy;
        public static string Mission = "easy";
        public static int Seed = System.Environment.TickCount & 0x7fffffff;
        public static string HunterName = "Caçador";
        public static string ServerUrl = "ws://localhost:5000/ws";

        /// <summary>Guilda em uso: local (offline) ou online. Null = partida rápida sem perfil.</summary>
        public static IProfileService ProfileService;

        /// <summary>Humano controlado pela IA (testes e demonstração).</summary>
        public static bool AutoPilot;

        /// <summary>Segundos entre ações da IA e dos monstros; 0 nos testes.</summary>
        public static float AiDelay = 0.4f;

        public static MatchOutcome LastOutcome;

        /// <summary>O que a última partida rendeu ao perfil (null numa partida rápida sem perfil).</summary>
        public static RewardSummary LastReward;

        public static void NewSeed() => Seed = (Seed * 1103515245 + 12345) & 0x7fffffff;

        /// <summary>Recompensa de vitória por missão, de data/missions.json.</summary>
        public static (int Gold, int Xp) RewardFor(string mission)
        {
            var content = ContentLoader.Load();
            if (!content.Missions.Contains(mission))
                return (100, 50);
            var m = content.Missions.Get(mission);
            return (m.RewardGold, m.RewardXp);
        }

        public static Color ColorOf(int index) => Presentation.Palette.Hunters[Mathf.Clamp(index, 0, Presentation.Palette.Hunters.Length - 1)];
    }
}
