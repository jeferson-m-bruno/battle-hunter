using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;

namespace BattleHunter.Client
{
    /// <summary>Consultas sobre o snapshot que a apresentação faz o tempo todo.</summary>
    public static class SnapshotExtensions
    {
        public static HunterSummary Hunter(this PlayerSnapshot s, int id) => s.Hunters.First(h => h.Id == id);

        public static string NameOf(this PlayerSnapshot s, int id) => s.Hunters.FirstOrDefault(h => h.Id == id)?.Name ?? $"#{id}";

        public static Chest ChestAt(this PlayerSnapshot s, Position p) => s.Chests.FirstOrDefault(c => c.Position == p);

        public static Monster MonsterAt(this PlayerSnapshot s, Position p) => s.Monsters.FirstOrDefault(m => m.Position == p);

        public static HunterSummary HunterAt(this PlayerSnapshot s, Position p) =>
            s.Hunters.FirstOrDefault(h => h.Status == HunterStatus.Active && h.Position == p);

        /// <summary>Ocupada por algo que este jogador vê (o servidor recusa o que ele não vê).</summary>
        public static bool IsOccupied(this PlayerSnapshot s, Position p) => s.MonsterAt(p) != null || s.HunterAt(p) != null;

        public static Position ExitCell(this PlayerSnapshot s)
        {
            for (var y = 0; y < s.Map.Height; y++)
                for (var x = 0; x < s.Map.Width; x++)
                {
                    var p = new Position(x, y);
                    if (s.Map[p] == Cell.Exit)
                        return p;
                }

            return null;
        }

        public static bool IsFinished(this PlayerSnapshot s) => s.Phase == GamePhase.Finished;

        /// <summary>Resumo para a tela de resultado, a partir do snapshot final.</summary>
        public static MatchOutcome ToOutcome(this PlayerSnapshot s, GameContent content, int missionGold, int missionXp)
        {
            var kept = s.Self.Hand.Take(GameSession.MaxCardsKept).ToList();
            var sold = s.Self.Hand.Skip(GameSession.MaxCardsKept).Sum(id => content.Cards.Get(id).Sell);
            var won = s.WinnerId == s.HunterId;
            return new MatchOutcome
            {
                Reason = s.EndReason ?? GameEndReason.RoundLimit,
                WinnerId = s.WinnerId,
                WinnerName = s.WinnerId != null ? s.NameOf(s.WinnerId.Value) : null,
                Rounds = s.Round,
                HumanStatus = s.Self.Status,
                CardsKept = kept,
                GoldFromCards = sold,
                MissionGold = won ? missionGold : 0,
                Xp = s.Self.Xp,
                MissionXp = won ? missionXp : 0,
            };
        }
    }
}
