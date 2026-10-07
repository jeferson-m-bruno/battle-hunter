using System;
using System.Collections.Generic;
using System.IO;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Rules;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using UnityEngine;

namespace BattleHunter.Client.Progression
{
    /// <summary>
    /// Perfil do modo offline: um JSON em persistentDataPath, com as mesmas regras do core que o servidor usa.
    /// Não sincroniza com o perfil online na v1.
    /// </summary>
    public sealed class LocalProfileService : IProfileService
    {
        private readonly string _path;
        private readonly GameContent _content;

        public LocalProfileService(GameContent content, string path = null)
        {
            _content = content;
            _path = path ?? Path.Combine(Application.persistentDataPath, "profile.json");
            Load();
        }

        public bool IsOnline => false;
        public Profile Profile { get; private set; }
        public int XpToNextLevel => Profile == null ? 0 : LevelRules.XpToNextLevel(Profile, _content.Levels);
        public string ShopDay => DateTimeOffset.Now.ToString("yyyy-MM-dd");
        public IReadOnlyList<string> ShopStock => Shop.StockFor(DateTimeOffset.Now, _content.Cards);
        public IReadOnlyList<RankingEntry> Ranking => Profile == null
            ? Array.Empty<RankingEntry>()
            : new[] { new RankingEntry(Profile.Id, Profile.Name, Profile.Level, Profile.RankPoints) };
        public string LastError { get; private set; } = "";

        public event Action OnChanged;

        public bool HasProfile => Profile != null;

        public void Refresh() => OnChanged?.Invoke();

        public void SetAppearance(string name, int colorIndex, int faceIndex)
        {
            var clean = string.IsNullOrWhiteSpace(name) ? "Caçador" : name.Trim();
            clean = clean.Substring(0, Math.Min(clean.Length, 16));
            Profile = (Profile ?? Profile.New(Guid.NewGuid().ToString("N"), clean, season: RankRules.SeasonOf(DateTimeOffset.Now)))
                with { Name = clean, ColorIndex = Mathf.Clamp(colorIndex, 0, 3), FaceIndex = Mathf.Clamp(faceIndex, 0, 3) };
            Save();
        }

        public void AllocatePoint(LevelRules.Stat stat) => Apply(LevelRules.AllocatePoint(Profile, stat), "Sem pontos livres.");

        public void SetLoadout(Loadout loadout)
        {
            var invalid = LoadoutRules.Validate(Profile, loadout, _content.Cards);
            if (invalid != null)
            {
                Fail(invalid);
                return;
            }

            Profile = Profile with { Loadout = loadout };
            Save();
        }

        public void RequestShop() => OnChanged?.Invoke();

        public void Buy(string cardId) => Apply(Shop.Buy(Profile, cardId, DateTimeOffset.Now, _content.Cards), "Fora do estoque do dia ou ouro insuficiente.");

        public void Sell(string cardId) => Apply(Shop.Sell(Profile, cardId, _content.Cards), "A carta não está livre no inventário.");

        public void Deposit(int amount) => Apply(Shop.Deposit(Profile, amount), "Valor inválido para depósito.");

        public void RequestRanking() => OnChanged?.Invoke();

        /// <summary>Fim de missão offline: aplica as recompensas do core e grava.</summary>
        public RewardSummary ApplyMatch(Hunter final, bool won, MissionType mission, IRandom random)
        {
            var (updated, summary) = MatchRewards.Apply(Profile, final, won, mission, _content, random, RankRules.SeasonOf(DateTimeOffset.Now));
            Profile = updated;
            Save();
            return summary;
        }

        private void Apply(Profile updated, string error)
        {
            if (updated == null)
            {
                Fail(error);
                return;
            }

            Profile = updated;
            Save();
        }

        private void Fail(string error)
        {
            LastError = error;
            OnChanged?.Invoke();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_path))
                    Profile = MessageJson.Deserialize<Profile>(File.ReadAllText(_path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Perfil local ilegível ({ex.Message}); começando do zero.");
                Profile = null;
            }
        }

        private void Save()
        {
            LastError = "";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_path, MessageJson.Serialize(Profile));
            }
            catch (Exception ex)
            {
                LastError = "Não consegui gravar o perfil: " + ex.Message;
            }

            OnChanged?.Invoke();
        }
    }
}
