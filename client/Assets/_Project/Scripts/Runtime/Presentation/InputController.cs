using System.Collections.Generic;
using System.Linq;
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
        private IGameHost _host;
        private BoardView _board;
        private HandView _hand;
        private HudView _hud;
        private readonly Queue<Position> _pendingSteps = new();
        private int _pendingPa = -1;

        public void Bind(IGameHost host, BoardView board, HandView hand, HudView hud)
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
            var v = _host?.View;
            if (v == null)
                return;

            if (_pendingSteps.Count > 0)
            {
                // Um passo por mudança de PA: espera o servidor/host confirmar o anterior.
                if (!_host.IsHumanTurn || v.Phase != GamePhase.Acting)
                {
                    _pendingSteps.Clear();
                }
                else if (v.ActionPoints != _pendingPa)
                {
                    _pendingPa = v.ActionPoints;
                    var next = _pendingSteps.Dequeue();
                    if (!_host.Submit(new MoveTo(_host.HumanId, next)))
                        _pendingSteps.Clear();
                }
                return;
            }

            if (!Tapped(out var screen))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!_host.IsHumanTurn)
                return;

            if (v.Phase == GamePhase.AwaitingRoll)
            {
                _host.Submit(new RollDice(_host.HumanId));
                return;
            }

            var world = Camera.main.ScreenToWorldPoint(screen);
            var cell = Iso.ToCell(new Vector3(world.x, world.y, 0f));
            if (!v.Map.IsInside(cell))
                return;

            Handle(v, cell);
        }

        private void Handle(Core.Serialization.PlayerSnapshot v, Position cell)
        {
            var me = v.Self;

            if (_hand.TargetingCard != null)
            {
                var card = _hand.TargetingCard;
                _hand.CancelTargeting();
                _host.Submit(new UseCard(_host.HumanId, card, cell));
                return;
            }

            if (cell == me.Position)
            {
                var ground = v.GroundCards.FirstOrDefault(g => g.Position == cell);
                if (ground != null)
                    _host.Submit(new PickUp(_host.HumanId, ground.CardId));
                return;
            }

            if (me.Position.IsOrthogonallyAdjacentTo(cell))
            {
                var chest = v.ChestAt(cell);
                if (chest != null && !chest.IsOpened)
                {
                    _host.Submit(new OpenChest(_host.HumanId, cell));
                    return;
                }

                if (v.MonsterAt(cell) != null || v.HunterAt(cell) != null)
                {
                    _host.Submit(new Attack(_host.HumanId, cell));
                    return;
                }
            }

            var path = Pathfinding.AStar(v.Map, me.Position, cell, p => v.Map.IsWalkable(p) && !v.IsOccupied(p));
            if (path == null || path.Count == 0 || path.Count > v.ActionPoints || !v.Map.IsWalkable(cell) || v.IsOccupied(cell))
            {
                _hud.Show("Fora do alcance.");
                return;
            }

            _pendingPa = -1;
            foreach (var step in path)
                _pendingSteps.Enqueue(step);
        }

        private void RefreshHighlights()
        {
            var v = _host.View;
            if (v == null || v.Phase != GamePhase.Acting || !_host.IsHumanTurn)
            {
                _board.Highlight(Enumerable.Empty<Position>(), Enumerable.Empty<Position>());
                return;
            }

            var me = v.Self;
            var reachable = Pathfinding.Distances(v.Map, me.Position, p => v.Map.IsWalkable(p) && !v.IsOccupied(p))
                .Where(kv => kv.Value > 0 && kv.Value <= v.ActionPoints)
                .Select(kv => kv.Key);

            var targets = Pathfinding.Neighbors(me.Position)
                .Where(p => v.Map.IsInside(p) && (v.MonsterAt(p) != null || v.HunterAt(p) != null || (v.ChestAt(p) is { IsOpened: false })));

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
