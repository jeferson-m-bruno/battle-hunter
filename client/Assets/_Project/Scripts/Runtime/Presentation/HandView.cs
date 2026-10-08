using System;
using System.Linq;
using BattleHunter.Client.UI;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using UnityEngine;
using UnityEngine.UI;

namespace BattleHunter.Client.Presentation
{
    /// <summary>Painel inferior: mão de cartas (oculta aos outros), equipamento, ações de carta e modal de descarte.</summary>
    public sealed class HandView : MonoBehaviour
    {
        private IGameHost _host;
        private RectTransform _panel;
        private RectTransform _cards;
        private RectTransform _popup;
        private Text _equipment;
        private Text _popupTitle;
        private string _selected;
        private bool _discardMode;

        /// <summary>Carta à espera de uma célula-alvo (bomba, arremesso, ataque especial).</summary>
        public string TargetingCard { get; private set; }

        public event Action<string> OnTargetingStarted;

        public void Build(Transform canvas, IGameHost host)
        {
            _host = host;
            _panel = Ui.PanelRect(canvas, "Hand", new Vector2(0f, 0f), new Vector2(1f, 0.18f));
            _equipment = Ui.Label(_panel, "Equipment", "", 24, TextAnchor.UpperLeft, new Vector2(0f, 0.78f), new Vector2(1f, 1f), new Vector2(20, 0), new Vector2(-20, -8), new Color(0.85f, 0.85f, 0.85f));
            _cards = Ui.PanelRect(_panel, "Cards", new Vector2(0f, 0f), new Vector2(1f, 0.78f), Color.clear, new Vector2(12, 12), new Vector2(-12, 0));
            var grid = _cards.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 120);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            _popup = Ui.PanelRect(canvas, "CardPopup", new Vector2(0.1f, 0.19f), new Vector2(0.9f, 0.34f), new Color(0.18f, 0.16f, 0.22f, 0.97f));
            _popupTitle = Ui.Label(_popup, "Title", "", 30, TextAnchor.UpperLeft, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(20, 0), new Vector2(-20, -12));
            Ui.Button(_popup, "Use", "Usar", new Vector2(0.02f, 0.08f), new Vector2(0.32f, 0.5f), UseSelected, fontSize: 28);
            Ui.Button(_popup, "Equip", "Equipar", new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.5f), () => Submit(new Equip(_host.HumanId, _selected)), fontSize: 28);
            Ui.Button(_popup, "Discard", "Descartar", new Vector2(0.68f, 0.08f), new Vector2(0.98f, 0.5f), () => Submit(new Discard(_host.HumanId, _selected)), fontSize: 28, color: new Color(0.5f, 0.2f, 0.2f));
            _popup.gameObject.SetActive(false);

            host.OnStateChanged += Refresh;
            host.OnEvent += e =>
            {
                if (e is Core.State.Events.ActionRejected r && r.Reason.StartsWith("Mão cheia"))
                    _discardMode = true;
            };
            Refresh();
        }

        public void CancelTargeting()
        {
            TargetingCard = null;
            _popup.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            var v = _host.View;
            if (v == null)
                return;

            var me = v.Self;
            var cards = _host.Content.Cards;

            string Slot(string id) => id == null ? "—" : cards.Get(id).Name;
            _equipment.text = $"Arma: {Slot(me.Equipment.Weapon)}   Armadura: {Slot(me.Equipment.Armor)}   Acessório: {Slot(me.Equipment.Accessory)}   Mão {me.Hand.Count}/{GameState.MaxHandSize}" + (_discardMode ? "   ⚠ descarte uma carta" : "");

            foreach (Transform child in _cards)
                Destroy(child.gameObject);

            foreach (var id in me.Hand)
            {
                var card = cards.Get(id);
                var caption = $"{card.Name}\n<size=20>{Describe(card)}</size>";
                var button = Ui.Button(_cards, "Card " + id, caption, Vector2.zero, Vector2.one, () => Select(id), fontSize: 24, color: ColorOf(card),
                    icon: SpriteCatalog.CardIcon(card.Type), iconTop: true, frame: SpriteCatalog.CardFrame());
                button.GetComponentInChildren<Text>().supportRichText = true;
            }

            if (_selected != null && !me.Hand.Contains(_selected))
            {
                _selected = null;
                _popup.gameObject.SetActive(false);
            }

            if (_discardMode && me.Hand.Count < GameState.MaxHandSize)
                _discardMode = false;
        }

        private void Select(string id)
        {
            if (!_host.IsHumanTurn)
                return;

            _selected = id;
            TargetingCard = null;
            var card = _host.Content.Cards.Get(id);
            _popupTitle.text = $"{card.Name} — {Describe(card)}  (venda {card.Sell})";
            _popup.gameObject.SetActive(true);
            _popup.Find("Use").gameObject.SetActive(card.IsUsable);
            _popup.Find("Equip").gameObject.SetActive(card.IsEquipment);
            _popup.Find("Discard").gameObject.SetActive(card.Type != CardType.Treasure);
        }

        private void UseSelected()
        {
            var card = _host.Content.Cards.Get(_selected);
            if (NeedsTarget(card))
            {
                TargetingCard = _selected;
                _popup.gameObject.SetActive(false);
                OnTargetingStarted?.Invoke(_selected);
                return;
            }

            Submit(new UseCard(_host.HumanId, _selected));
        }

        private void Submit(GameAction action)
        {
            _popup.gameObject.SetActive(false);
            _host.Submit(action);
        }

        public static bool NeedsTarget(Card card) =>
            card.Type == CardType.SpecialAttack || card.Effect is "bomb" or "throw";

        private static string Describe(Card card)
        {
            if (card.IsEquipment)
            {
                var m = card.Mods;
                var parts = new[] { (m.Atk, "ATQ"), (m.Def, "DEF"), (m.Spd, "VEL"), (m.Luck, "SOR"), (m.Hp, "PV") }
                    .Where(p => p.Item1 != 0).Select(p => $"{p.Item1:+#;-#} {p.Item2}");
                return string.Join(" ", parts);
            }

            if (card.Type == CardType.Treasure)
                return "tesouro";

            var p2 = string.Join(" ", card.Params.Select(kv => $"{kv.Key} {kv.Value}"));
            return $"{card.Effect} {p2}".Trim();
        }

        private static Color ColorOf(Card card) => card.Type switch
        {
            CardType.Treasure => new Color(0.6f, 0.5f, 0.1f),
            CardType.Consumable => new Color(0.2f, 0.4f, 0.3f),
            CardType.Trap => new Color(0.4f, 0.25f, 0.2f),
            CardType.SpecialAttack => new Color(0.45f, 0.2f, 0.3f),
            _ => Ui.ButtonColor,
        };
    }
}
