using System.Collections.Generic;
using BattleHunter.Core.Ai;
using BattleHunter.Core.State;
using UnityEngine;

namespace BattleHunter.Client
{
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

    /// <summary>Estado que atravessa cenas: escolhas do jogador, seed da próxima partida e o último resultado.</summary>
    public static class GameSession
    {
        public const int HumanHunterId = 1;
        public const int MaxCardsKept = 5;

        public static MatchSettings Settings = MatchSettings.Easy;
        public static int Seed = System.Environment.TickCount & 0x7fffffff;
        public static string HunterName = "Caçador";
        public static Color HunterColor = new Color(0.95f, 0.75f, 0.2f);

        /// <summary>Humano controlado pela IA (testes e demonstração).</summary>
        public static bool AutoPilot;

        /// <summary>Segundos entre ações da IA e dos monstros; 0 nos testes.</summary>
        public static float AiDelay = 0.4f;

        public static MatchOutcome LastOutcome;

        public static void NewSeed() => Seed = (Seed * 1103515245 + 12345) & 0x7fffffff;
    }
}
