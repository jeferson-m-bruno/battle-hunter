using System;
using System.Collections.Generic;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Serialization;

namespace BattleHunter.Client.Progression
{
    /// <summary>
    /// A guilda vista pelo cliente: perfil, loja, loadout, ranking. Implementação local (JSON no dispositivo, offline)
    /// ou online (mensagens ao servidor). As telas só falam com esta interface.
    /// </summary>
    public interface IProfileService
    {
        bool IsOnline { get; }
        Profile Profile { get; }
        int XpToNextLevel { get; }
        string ShopDay { get; }
        IReadOnlyList<string> ShopStock { get; }
        IReadOnlyList<RankingEntry> Ranking { get; }
        string LastError { get; }

        event Action OnChanged;

        void Refresh();
        void SetAppearance(string name, int colorIndex, int faceIndex);
        void AllocatePoint(LevelRules.Stat stat);
        void SetLoadout(Loadout loadout);
        void RequestShop();
        void Buy(string cardId);
        void Sell(string cardId);
        void Deposit(int amount);
        void RequestRanking();
    }
}
