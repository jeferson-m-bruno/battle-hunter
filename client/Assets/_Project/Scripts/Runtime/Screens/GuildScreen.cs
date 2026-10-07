using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Client.Net;
using BattleHunter.Client.Progression;
using BattleHunter.Client.UI;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Progression;
using BattleHunter.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleHunter.Client.Screens
{
    /// <summary>
    /// Guilda (GDD): hub entre partidas — criar o caçador, escolher missão (com requisito de nível), equipar e escolher
    /// as cartas levadas, loja diária, vender, depositar, ranking e gastar pontos livres. Mesma tela para o perfil local e o online.
    /// </summary>
    public sealed class GuildScreen : MonoBehaviour
    {
        private enum Tab { Missions, Loadout, Shop, Ranking }

        private IProfileService _service;
        private GameContent _content;
        private Transform _canvas;
        private Text _header;
        private Text _message;
        private RectTransform _points;
        private RectTransform _tabs;
        private RectTransform _body;
        private Tab _tab = Tab.Missions;
        private Loadout _draft;
        private float _messageUntil;

        private void Start()
        {
            _content = ContentLoader.Load();
            _canvas = Ui.Canvas("GuildCanvas").transform;
            Ui.PanelRect(_canvas, "Bg", Vector2.zero, Vector2.one, Ui.Background);

            _service = GameSession.ProfileService ?? (GameSession.ProfileService = new LocalProfileService(_content));
            _service.OnChanged += Redraw;

            if (_service.Profile == null && _service is LocalProfileService)
            {
                BuildCreateHunter();
                return;
            }

            BuildGuild();
            _service.Refresh();
            _service.RequestShop();
        }

        private void OnDestroy()
        {
            if (_service != null)
                _service.OnChanged -= Redraw;
        }

        private void Update()
        {
            if (_message != null && Time.time > _messageUntil && _message.text != "")
                _message.text = "";
        }

        // ---- criação do caçador --------------------------------------------------

        private void BuildCreateHunter()
        {
            Ui.Label(_canvas, "Title", "Novo caçador", 72, TextAnchor.MiddleCenter, new Vector2(0f, 0.84f), new Vector2(1f, 0.94f), color: Ui.Accent);
            Ui.Label(_canvas, "NameLabel", "Nome", 28, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.74f), new Vector2(0.9f, 0.78f));
            var name = Ui.Field(_canvas, "Name", GameSession.HunterName, new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.735f));

            var color = 0;
            var face = 0;
            Ui.Label(_canvas, "ColorLabel", "Cor", 28, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.62f));
            var colorButtons = new List<Button>();
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var b = Ui.Button(_canvas, $"Color{i}", "", new Vector2(0.1f + i * 0.2f, 0.5f), new Vector2(0.26f + i * 0.2f, 0.57f), () => { color = index; Mark(colorButtons, index); }, color: Presentation.Palette.Hunters[i]);
                colorButtons.Add(b);
            }

            Ui.Label(_canvas, "FaceLabel", "Rosto", 28, TextAnchor.MiddleLeft, new Vector2(0.1f, 0.41f), new Vector2(0.9f, 0.45f));
            var faces = new[] { "◉", "◈", "◆", "✦" };
            var faceButtons = new List<Button>();
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var b = Ui.Button(_canvas, $"Face{i}", faces[i], new Vector2(0.1f + i * 0.2f, 0.33f), new Vector2(0.26f + i * 0.2f, 0.40f), () => { face = index; Mark(faceButtons, index); }, fontSize: 48);
                faceButtons.Add(b);
            }

            Mark(colorButtons, 0);
            Mark(faceButtons, 0);
            Ui.Label(_canvas, "Note", "Cor, rosto e nome não mudam as regras (GDD).", 24, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.3f), color: new Color(0.7f, 0.7f, 0.7f));

            Ui.Button(_canvas, "Create", "Entrar na guilda", new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.22f), () =>
            {
                GameSession.HunterName = string.IsNullOrWhiteSpace(name.text) ? "Caçador" : name.text.Trim();
                _service.SetAppearance(GameSession.HunterName, color, face);
                Ui.Clear(_canvas);
                Ui.PanelRect(_canvas, "Bg", Vector2.zero, Vector2.one, Ui.Background);
                BuildGuild();
                _service.RequestShop();
            });
            Ui.Button(_canvas, "Back", "Voltar", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), () => SceneManager.LoadScene("Boot"), fontSize: 28);
        }

        private static void Mark(List<Button> buttons, int selected)
        {
            for (var i = 0; i < buttons.Count; i++)
                buttons[i].transform.localScale = i == selected ? Vector3.one * 1.12f : Vector3.one;
        }

        // ---- guilda ---------------------------------------------------------------

        private void BuildGuild()
        {
            var top = Ui.PanelRect(_canvas, "Top", new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            _header = Ui.Label(top, "Header", "", 28, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(0.78f, 1f), new Vector2(20, 8), new Vector2(0, -12));
            Ui.Button(top, "Back", "Sair", new Vector2(0.8f, 0.55f), new Vector2(0.98f, 0.92f), () =>
            {
                if (_service is OnlineProfileService)
                    SceneManager.LoadScene("Lobby");
                else
                    SceneManager.LoadScene("Boot");
            }, fontSize: 26);
            _points = Ui.PanelRect(top, "Points", new Vector2(0f, 0f), new Vector2(1f, 0.3f), Color.clear);

            _tabs = Ui.PanelRect(_canvas, "Tabs", new Vector2(0f, 0.79f), new Vector2(1f, 0.855f), new Color(0, 0, 0, 0.35f));
            var names = new[] { ("Missões", Tab.Missions), ("Equipar", Tab.Loadout), ("Loja", Tab.Shop), ("Ranking", Tab.Ranking) };
            for (var i = 0; i < names.Length; i++)
            {
                var (caption, tab) = names[i];
                Ui.Button(_tabs, "Tab " + caption, caption, new Vector2(0.01f + i * 0.245f, 0.1f), new Vector2(0.245f + i * 0.245f, 0.9f), () => Select(tab), fontSize: 26);
            }

            _message = Ui.Label(_canvas, "Message", "", 26, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.73f), new Vector2(0.98f, 0.785f), color: Ui.Accent);
            _body = Ui.PanelRect(_canvas, "Body", new Vector2(0f, 0f), new Vector2(1f, 0.73f), Color.clear);
            Redraw();
        }

        private void Select(Tab tab)
        {
            _tab = tab;
            _draft = null;
            if (tab == Tab.Shop)
                _service.RequestShop();
            if (tab == Tab.Ranking)
                _service.RequestRanking();
            Redraw();
        }

        private void Show(string text)
        {
            if (_message == null)
                return;
            _message.text = text;
            _messageUntil = Time.time + 3f;
        }

        private void Redraw()
        {
            if (_body == null || _service.Profile == null)
                return;

            if (!string.IsNullOrEmpty(_service.LastError))
                Show(_service.LastError);

            var p = _service.Profile;
            var stats = LevelRules.StatsFor(p);
            var next = _service.XpToNextLevel;
            _header.text = $"{p.Name}  ·  nível {p.Level}  ·  XP {p.Xp}" + (next > 0 ? $" (faltam {next})" : " (máximo)") +
                           $"\nPV {stats.MaxHp}  ATQ {stats.Attack}  DEF {stats.Defense}  VEL {stats.Speed}  SOR {stats.Luck}" +
                           $"   Ouro {p.Gold} (banco {p.BankedGold})   Rank {p.RankPoints}" + (_service.IsOnline ? "  ·  online" : "  ·  local");

            Ui.Clear(_points);
            if (p.UnspentPoints > 0)
            {
                Ui.Label(_points, "PtsLabel", $"{p.UnspentPoints} ponto(s) livre(s):", 24, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.34f, 1f), new Vector2(20, 0), Vector2.zero, Ui.Accent);
                var stat = new[] { ("+PV", LevelRules.Stat.Hp), ("+ATQ", LevelRules.Stat.Atk), ("+DEF", LevelRules.Stat.Def), ("+VEL", LevelRules.Stat.Spd), ("+SOR", LevelRules.Stat.Luck) };
                for (var i = 0; i < stat.Length; i++)
                {
                    var (caption, s) = stat[i];
                    Ui.Button(_points, "Pt" + caption, caption, new Vector2(0.35f + i * 0.128f, 0.1f), new Vector2(0.47f + i * 0.128f, 0.95f), () => _service.AllocatePoint(s), fontSize: 22, color: new Color(0.3f, 0.45f, 0.3f));
                }
            }

            Ui.Clear(_body);
            switch (_tab)
            {
                case Tab.Missions: DrawMissions(p); break;
                case Tab.Loadout: DrawLoadout(p); break;
                case Tab.Shop: DrawShop(p); break;
                case Tab.Ranking: DrawRanking(p); break;
            }
        }

        private void DrawMissions(Profile p)
        {
            var list = Ui.ScrollList(_body, "Missions", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.98f), 150f);
            foreach (var m in _content.Missions.All)
            {
                var locked = p.Level < m.RequiredLevel;
                var text = $"<b>{m.Name}</b>  ({m.GridSize}×{m.GridSize}, {m.MaxRounds} rodadas{(m.Boss ? ", chefe" : "")})\n" +
                           $"{m.RewardGold} ouro + {m.RewardXp} XP{(m.RewardRareCard ? " + carta rara" : "")}{(m.Ranked ? " + pontos de rank" : "")}" +
                           (locked ? $"\n<color=#ff8080>exige nível {m.RequiredLevel}</color>" : "");
                Ui.ListItem(list, "Mission " + m.Id, text, null, locked ? null : "Partir", () => Depart(m), locked ? new Color(0.2f, 0.19f, 0.22f) : (Color?)null);
            }
        }

        private void Depart(MissionType mission)
        {
            var invalid = LoadoutRules.Validate(_service.Profile, _service.Profile.Loadout, _content.Cards);
            if (invalid != null)
            {
                Show("Loadout inválido: " + invalid);
                return;
            }

            GameSession.Mission = mission.Id;
            GameSession.HunterName = _service.Profile.Name;
            GameSession.LastReward = null;

            if (_service is OnlineProfileService)
            {
                var host = FindFirstObjectByType<OnlineGameHost>();
                if (host == null)
                {
                    SceneManager.LoadScene("Lobby");
                    return;
                }

                GameSession.Mode = GameMode.Online;
                host.JoinQueue(mission.Id);
                SceneManager.LoadScene("Lobby");
                return;
            }

            GameSession.Mode = GameMode.Offline;
            GameSession.NewSeed();
            SceneManager.LoadScene("Partida");
        }

        private void DrawLoadout(Profile p)
        {
            _draft ??= p.Loadout;
            var cards = _content.Cards;
            string Slot(string id) => id == null ? "—" : cards.Get(id).Name;

            Ui.Label(_body, "Slots", $"Arma: {Slot(_draft.Equipment.Weapon)}   Armadura: {Slot(_draft.Equipment.Armor)}   Acessório: {Slot(_draft.Equipment.Accessory)}\nMão ({_draft.Hand.Count}/{Loadout.MaxHand}): {(_draft.Hand.Count == 0 ? "vazia" : string.Join(", ", _draft.Hand.Select(id => cards.Get(id).Name)))}",
                24, TextAnchor.UpperLeft, new Vector2(0.03f, 0.84f), new Vector2(0.97f, 0.98f));

            var list = Ui.ScrollList(_body, "Inventory", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.83f), 100f);
            var available = p.Inventory.ToList();
            foreach (var id in _draft.Hand.Concat(_draft.EquippedIds()))
                available.Remove(id);

            foreach (var group in available.GroupBy(id => id).OrderBy(g => cards.Get(g.Key).Type).ThenBy(g => cards.Get(g.Key).Name))
            {
                var card = cards.Get(group.Key);
                var count = group.Count() > 1 ? $" ×{group.Count()}" : "";
                var text = $"<b>{card.Name}</b>{count}  <size=20>{Describe(card)}</size>";
                var action = card.IsEquipment ? "Equipar" : card.IsUsable ? "Levar" : null;
                Ui.ListItem(list, "Inv " + card.Id, text, null, action, () =>
                {
                    if (card.IsEquipment)
                        _draft = _draft with { Equipment = _draft.Equipment.With(Equipment.SlotFor(card.Type), card.Id) };
                    else if (_draft.Hand.Count < Loadout.MaxHand)
                        _draft = _draft with { Hand = _draft.Hand.Append(card.Id).ToList() };
                    else
                        Show("A mão leva no máximo 5 cartas.");
                    Redraw();
                });
            }

            if (available.Count == 0)
                Ui.Label(list, "Empty", "Inventário vazio: ganhe cartas nas missões ou compre na loja.", 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            Ui.Button(_body, "ClearHand", "Esvaziar mão", new Vector2(0.03f, 0.02f), new Vector2(0.3f, 0.1f), () => { _draft = _draft with { Hand = Array.Empty<string>() }; Redraw(); }, fontSize: 24);
            Ui.Button(_body, "Unequip", "Tirar equipamento", new Vector2(0.33f, 0.02f), new Vector2(0.64f, 0.1f), () => { _draft = _draft with { Equipment = Equipment.None }; Redraw(); }, fontSize: 24);
            Ui.Button(_body, "SaveLoadout", "Salvar", new Vector2(0.67f, 0.02f), new Vector2(0.97f, 0.1f), () => { _service.SetLoadout(_draft); Show("Loadout salvo."); }, fontSize: 24, color: new Color(0.2f, 0.5f, 0.3f));
        }

        private void DrawShop(Profile p)
        {
            var cards = _content.Cards;
            Ui.Label(_body, "ShopTitle", $"Estoque de {_service.ShopDay} (compra = 3× venda)   Ouro: {p.Gold}", 26, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.98f));
            var stock = Ui.ScrollList(_body, "Stock", new Vector2(0.03f, 0.52f), new Vector2(0.97f, 0.91f), 90f);
            foreach (var id in _service.ShopStock)
            {
                var card = cards.Get(id);
                Ui.ListItem(stock, "Buy " + id, $"<b>{card.Name}</b>  <size=20>{Describe(card)}</size>", null, $"{Shop.PriceOf(card)} ouro", () => _service.Buy(id), fontSize: 24);
            }

            Ui.Label(_body, "SellTitle", "Vender do inventário (cartas fora do loadout)", 26, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.45f), new Vector2(0.97f, 0.51f));
            var sell = Ui.ScrollList(_body, "Sell", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.44f), 90f);
            var free = p.Inventory.ToList();
            foreach (var id in p.Loadout.Hand.Concat(p.Loadout.EquippedIds()))
                free.Remove(id);
            foreach (var group in free.GroupBy(id => id).OrderBy(g => cards.Get(g.Key).Name))
            {
                var card = cards.Get(group.Key);
                Ui.ListItem(sell, "Sell " + card.Id, $"<b>{card.Name}</b>{(group.Count() > 1 ? $" ×{group.Count()}" : "")}", null, $"+{card.Sell}", () => _service.Sell(card.Id), fontSize: 24);
            }

            Ui.Button(_body, "Deposit", $"Depositar tudo ({p.Gold})", new Vector2(0.03f, 0.02f), new Vector2(0.48f, 0.1f), () => { if (p.Gold > 0) _service.Deposit(p.Gold); }, fontSize: 24);
            Ui.Label(_body, "DepositNote", "Ouro depositado não se perde ao cair.", 22, TextAnchor.MiddleLeft, new Vector2(0.52f, 0.02f), new Vector2(0.97f, 0.1f), color: new Color(0.7f, 0.7f, 0.7f));
        }

        private void DrawRanking(Profile p)
        {
            var season = p.Season == "" ? RankRules.SeasonOf(DateTimeOffset.Now) : p.Season;
            Ui.Label(_body, "RankTitle", $"Temporada {season}  ·  seus pontos: {p.RankPoints}" + (_service.IsOnline ? "" : "  (ranking global só online)"), 26, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.98f));
            var list = Ui.ScrollList(_body, "Ranking", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.91f), 80f);
            var i = 1;
            foreach (var e in _service.Ranking)
                Ui.ListItem(list, "Rank " + e.PlayerId, $"{i++}. <b>{e.Name}</b>  nível {e.Level}", null, $"{e.RankPoints} pts", null, e.PlayerId == p.Id ? new Color(0.3f, 0.35f, 0.5f) : (Color?)null, 24);
            if (_service.Ranking.Count == 0)
                Ui.Label(list, "Empty", "Ninguém pontuou nesta temporada ainda.", 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        }

        private static string Describe(Card card)
        {
            if (card.IsEquipment)
            {
                var m = card.Mods;
                var parts = new[] { (m.Atk, "ATQ"), (m.Def, "DEF"), (m.Spd, "VEL"), (m.Luck, "SOR"), (m.Hp, "PV") }
                    .Where(x => x.Item1 != 0).Select(x => $"{x.Item1:+#;-#} {x.Item2}");
                return string.Join(" ", parts);
            }

            if (card.Type == CardType.Treasure)
                return "tesouro";
            return $"{card.Effect} {string.Join(" ", card.Params.Select(kv => $"{kv.Key} {kv.Value}"))}".Trim();
        }
    }
}
