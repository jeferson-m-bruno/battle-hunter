using System;
using System.Collections.Generic;
using BattleHunter.Client.Progression;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Serialization;

namespace BattleHunter.Client.Net
{
    /// <summary>Guilda online: cada operação vira uma mensagem; o servidor responde com ProfileState/ShopState/Ranking.</summary>
    public sealed class OnlineProfileService : IProfileService
    {
        private readonly OnlineGameHost _host;

        public OnlineProfileService(OnlineGameHost host)
        {
            _host = host;
            host.OnGuildMessage += Handle;
        }

        public bool IsOnline => true;
        public Profile Profile { get; private set; }
        public int XpToNextLevel { get; private set; }
        public string ShopDay { get; private set; } = "";
        public IReadOnlyList<string> ShopStock { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<RankingEntry> Ranking { get; private set; } = Array.Empty<RankingEntry>();
        public string LastError { get; private set; } = "";

        public event Action OnChanged;

        public void Refresh() => _host.Send(new ProfileRequest());
        public void SetAppearance(string name, int colorIndex, int faceIndex) => _host.Send(new SetAppearance(name, colorIndex, faceIndex));
        public void AllocatePoint(LevelRules.Stat stat) => _host.Send(new AllocatePoint(stat.ToString().ToLowerInvariant()));
        public void SetLoadout(Loadout loadout) => _host.Send(new SetLoadout(loadout));
        public void RequestShop() => _host.Send(new ShopRequest());
        public void Buy(string cardId) => _host.Send(new Buy(cardId));
        public void Sell(string cardId) => _host.Send(new Sell(cardId));
        public void Deposit(int amount) => _host.Send(new Deposit(amount));
        public void RequestRanking() => _host.Send(new RankingRequest());

        private void Handle(ServerMessage message)
        {
            switch (message)
            {
                case ProfileState ps:
                    Profile = ps.Profile;
                    XpToNextLevel = ps.XpToNextLevel;
                    LastError = "";
                    break;
                case MatchRewarded mr:
                    Profile = mr.Profile;
                    break;
                case ShopState ss:
                    ShopDay = ss.Day;
                    ShopStock = ss.Stock;
                    break;
                case Ranking r:
                    Ranking = r.Entries;
                    break;
                case Error e:
                    LastError = e.Message;
                    break;
                default:
                    return;
            }

            OnChanged?.Invoke();
        }
    }
}
