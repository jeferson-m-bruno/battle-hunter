using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Client.Progression;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.Progression;
using BattleHunter.Core.Rules;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using UnityEngine;

namespace BattleHunter.Client.Offline
{
    /// <summary>
    /// Roda o core localmente: 1 humano + 3 IAs. Nenhuma regra aqui — o host só aplica intenções no Reducer,
    /// publica os eventos para a apresentação e dá o ritmo (atraso da IA, timer de 45 s do turno humano).
    /// Com perfil local, o humano entra com nível, pontos, equipamento e cartas da guilda e recebe as recompensas no fim.
    /// </summary>
    public sealed class OfflineGameHost : MonoBehaviour, IGameHost
    {
        public const float TurnSeconds = 45f;

        public GameState State { get; private set; }
        public GameContent Content { get; private set; }
        public int HumanId => GameSession.HumanHunterId;
        public PlayerSnapshot View { get; private set; }
        public bool IsHumanTurn => State != null && State.Phase is not GamePhase.NotStarted and not GamePhase.Finished && State.CurrentHunterId == HumanId && !GameSession.AutoPilot;
        public float TurnTimeLeft { get; private set; } = TurnSeconds;
        public string Title => $"offline · {GameSession.Mission} · seed {GameSession.Seed}";
        public IReadOnlyDictionary<int, AiProfile> Profiles => _profiles;

        public event Action<GameEvent> OnEvent;
        public event Action OnStateChanged;
        public event Action<MatchOutcome> OnFinished;

        private IRandom _random;
        private readonly Dictionary<int, AiPlayer> _ais = new();
        private readonly Dictionary<int, AiProfile> _profiles = new();
        private bool _busy;

        private void Start()
        {
            Content = ContentLoader.Load();
            _random = new SeededRandom(GameSession.Seed);

            var mission = Content.Missions.Contains(GameSession.Mission) ? Content.Missions.Get(GameSession.Mission) : Content.Missions.Get("easy");
            var settings = MatchSettings.For(mission);
            GameSession.Settings = settings;
            var map = MapGenerator.Generate(settings.Map, _random);

            var commons = Content.Cards.Where(c => c.Rarity == Rarity.Common && c.Type != CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
            var treasures = Content.Cards.Where(c => c.Type == CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
            var aiProfiles = new[] { AiProfile.Aggressive, AiProfile.Cautious, AiProfile.Greedy, AiProfile.Balanced };
            var local = GameSession.ProfileService as LocalProfileService;

            var setups = new List<HunterSetup>();
            for (var i = 1; i <= 4; i++)
            {
                var profile = i == HumanId ? AiProfile.Balanced : aiProfiles[_random.Next(aiProfiles.Length)];
                _profiles[i] = profile;

                if (i == HumanId && local?.Profile != null)
                {
                    setups.Add(LoadoutRules.ToHunterSetup(local.Profile, i));
                }
                else
                {
                    var hand = Enumerable.Range(0, settings.StartingCommonCards).Select(_ => commons[_random.Next(commons.Count)].Id).ToList();
                    var name = i == HumanId ? GameSession.HunterName : $"IA {profile}";
                    setups.Add(new HunterSetup(i, name, HunterStats.Base, hand));
                }

                if (i != HumanId || GameSession.AutoPilot)
                    _ais[i] = new AiPlayer(i, profile);
            }

            var treasure = treasures[_random.Next(treasures.Count)].Id;
            State = GameSetup.Create(settings.Config, map, setups, treasure, _random, Content);
            Publish();

            Apply(new StartGame());
            StartCoroutine(Loop());
        }

        private void Update()
        {
            if (!IsHumanTurn || _busy)
                return;

            TurnTimeLeft -= Time.deltaTime;
            if (TurnTimeLeft > 0f)
                return;

            // 45 s: rola e/ou passa automaticamente.
            Submit(State.Phase == GamePhase.AwaitingRoll ? new RollDice(HumanId) : new Pass(HumanId));
        }

        /// <summary>Intenção do jogador humano. Ignorada fora do turno dele.</summary>
        public bool Submit(GameAction action)
        {
            if (State == null || State.Phase is GamePhase.NotStarted or GamePhase.Finished || State.CurrentHunterId != HumanId || _busy)
                return false;

            return Apply(action);
        }

        private IEnumerator Loop()
        {
            while (State.Phase != GamePhase.Finished)
            {
                var current = State.CurrentHunterId;
                if (!_ais.TryGetValue(current, out var ai))
                {
                    yield return null;
                    continue;
                }

                _busy = true;
                if (GameSession.AiDelay > 0f)
                    yield return new WaitForSeconds(GameSession.AiDelay);
                _busy = false;

                if (State.Phase == GamePhase.Finished || State.CurrentHunterId != current)
                    continue;

                var action = ai.Next(State, Content);
                if (!Apply(action))
                    Apply(State.Phase == GamePhase.AwaitingRoll ? new RollDice(current) : new Pass(current));
            }
        }

        private bool Apply(GameAction action)
        {
            var before = State;
            var result = Reducer.Apply(State, action, _random, Content);
            State = result.State;

            var rejected = result.Events.OfType<ActionRejected>().FirstOrDefault();
            if (rejected != null)
            {
                OnEvent?.Invoke(rejected);
                return false;
            }

            var turnChanged = before.Phase == GamePhase.NotStarted || before.TurnIndex != State.TurnIndex || before.Round != State.Round;
            if (turnChanged || State.Phase == GamePhase.AwaitingRoll)
                TurnTimeLeft = TurnSeconds;

            Publish();
            foreach (var e in EventFilter.ForRecipient(result.Events, State, HumanId, Content))
                OnEvent?.Invoke(e);
            OnStateChanged?.Invoke();

            if (State.Phase == GamePhase.Finished)
                Finish();

            return true;
        }

        private void Finish()
        {
            var reward = GameSession.RewardFor(GameSession.Mission);
            GameSession.LastOutcome = View.ToOutcome(Content, reward.Gold, reward.Xp);
            GameSession.LastReward = null;

            if (GameSession.ProfileService is LocalProfileService local && local.Profile != null)
            {
                var mission = Content.Missions.Contains(GameSession.Mission) ? Content.Missions.Get(GameSession.Mission) : Content.Missions.Get("easy");
                GameSession.LastReward = local.ApplyMatch(State.Hunter(HumanId), State.WinnerId == HumanId, mission, _random);
            }

            OnFinished?.Invoke(GameSession.LastOutcome);
        }

        private void Publish() => View = PlayerSnapshot.For(State, HumanId, Content);
    }
}
