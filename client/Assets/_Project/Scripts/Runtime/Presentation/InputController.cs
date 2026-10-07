using System.Collections.Generic;
using System.Linq;
using BattleHunter.Client.Offline;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BattleHunter.Client.Presentation
{
    /// <summary>
    /// Toque → intenção. Célula alcançável move (passo a passo pelo A*), baú adjacente abre, alvo adjacente ataca,
    /// a própria célula com cartas no chão pega, dado em "aguardando rolagem" rola. Nada de regra aqui: o Reducer decide.
    /// </summary>
    public sealed class InputController : MonoBehaviour
    {
        private OfflineGameHost _host;
        private BoardView _board;
        private HandView _hand;
        private HudView _hud;
        private readonly Queue<Position> _pendingSteps = new();

        public void Bind(OfflineGameHost host, BoardView board, HandView hand, HudView hud)
        {
            _host = host;
            _board = board;
            _hand = hand;
            _hud = hud;
            host.OnStateChanged += RefreshHighlights;
            hand.OnTargetingStarted += _ => RefreshHighlights();
        }

        private void Update()
        {
            if (_host == null || _host.State == null)
                return;

            if (_pendingSteps.Count > 0 && _host.IsHumanTurn && _host.State.Phase == GamePhase.Acting)
            {
                var next = _pendingSteps.Dequeue();
                if (!_host.Submit(new MoveTo(_host.HumanId, next)))
                    _pendingSteps.Clear();
                return;
            }

            if (!Tapped(out var screen))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!_host.IsHumanTurn)
                return;

            var state = _host.State;
            if (state.Phase == GamePhase.AwaitingRoll)
            {
                _host.Submit(new RollDice(_host.HumanId));
                return;
            }

            var world = Camera.main.ScreenToWorldPoint(screen);
            var cell = Iso.ToCell(new Vector3(world.x, world.y, 0f));
            if (!state.Map.IsInside(cell))
                return;

            Handle(state, cell);
        }

        private void Handle(GameState state, Position cell)
        {
            var me = state.Hunter(_host.HumanId);

            if (_hand.TargetingCard != null)
            {
                var card = _hand.TargetingCard;
                _hand.CancelTargeting();
                _host.Submit(new UseCard(_host.HumanId, card, cell));
                return;
            }

            if (cell == me.Position)
            {
                var ground = state.GroundCards.FirstOrDefault(g => g.Position == cell);
                if (ground != null)
                    _host.Submit(new PickUp(_host.HumanId, ground.CardId));
                return;
            }

            if (me.Position.IsOrthogonallyAdjacentTo(cell))
            {
                if (state.ChestAt(cell) is { IsOpened: false })
                {
                    _host.Submit(new OpenChest(_host.HumanId, cell));
                    return;
                }

                if (state.MonsterAt(cell) != null || state.HunterAt(cell) != null)
                {
                    _host.Submit(new Attack(_host.HumanId, cell));
                    return;
                }
            }

            var path = Pathfinding.AStar(state.Map, me.Position, cell, p => state.Map.IsWalkable(p) && !state.IsOccupied(p));
            if (path == null || path.Count == 0 || path.Count > state.ActionPoints || !state.Map.IsWalkable(cell) || state.IsOccupied(cell))
            {
                _hud.Show("Fora do alcance.");
                return;
            }

            foreach (var step in path)
                _pendingSteps.Enqueue(step);
        }

        private void RefreshHighlights()
        {
            var state = _host.State;
            if (state == null || state.Phase != GamePhase.Acting || !_host.IsHumanTurn)
            {
                _board.Highlight(Enumerable.Empty<Position>(), Enumerable.Empty<Position>());
                return;
            }

            var me = state.Hunter(_host.HumanId);
            var reachable = Pathfinding.Distances(state.Map, me.Position, p => state.Map.IsWalkable(p) && !state.IsOccupied(p))
                .Where(kv => kv.Value > 0 && kv.Value <= state.ActionPoints)
                .Select(kv => kv.Key);

            var view = HunterView.For(state, _host.HumanId, _host.Content);
            var targets = Pathfinding.Neighbors(me.Position)
                .Where(p => state.Map.IsInside(p) && (view.Monsters.Any(m => m.Position == p) || view.Others.Any(o => o.Position == p) || state.ChestAt(p) is { IsOpened: false }));

            _board.Highlight(_hand.TargetingCard != null ? Enumerable.Empty<Position>() : reachable, targets);
        }

        private static bool Tapped(out Vector3 screen)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                screen = Input.GetTouch(0).position;
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                screen = Input.mousePosition;
                return true;
            }

            screen = default;
            return false;
        }
    }
}
