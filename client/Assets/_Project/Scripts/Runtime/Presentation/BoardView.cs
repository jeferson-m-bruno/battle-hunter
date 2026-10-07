using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Ai;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;
using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>
    /// Desenha o tabuleiro isométrico a partir do GameState e anima os eventos. Só lê o estado; nunca decide regra.
    /// Camadas: 0 chão, 1 névoa/realce, 2 cartas no chão, 3 baús, 4 criaturas, 5 textos flutuantes.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private const float MoveSeconds = 0.15f;

        private GameContent _content;
        private int _viewerId;
        private readonly Dictionary<Position, SpriteRenderer> _tiles = new();
        private readonly Dictionary<Position, SpriteRenderer> _fog = new();
        private readonly Dictionary<Position, SpriteRenderer> _highlights = new();
        private readonly Dictionary<Position, SpriteRenderer> _chests = new();
        private readonly Dictionary<Position, SpriteRenderer> _ground = new();
        private readonly Dictionary<int, SpriteRenderer> _hunters = new();
        private readonly Dictionary<int, SpriteRenderer> _hunterMarks = new();
        private readonly Dictionary<int, SpriteRenderer> _monsters = new();
        private Transform _root;
        private Font _font;

        public void Build(GameState state, GameContent content, int viewerId)
        {
            _content = content;
            _viewerId = viewerId;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (_root != null)
                Destroy(_root.gameObject);
            _root = new GameObject("Board").transform;
            _root.SetParent(transform, false);
            _tiles.Clear(); _fog.Clear(); _highlights.Clear(); _chests.Clear(); _ground.Clear(); _hunters.Clear(); _hunterMarks.Clear(); _monsters.Clear();

            var map = state.Map;
            for (var y = 0; y < map.Height; y++)
                for (var x = 0; x < map.Width; x++)
                {
                    var p = new Position(x, y);
                    var cell = map[p];
                    var color = cell switch
                    {
                        Cell.Wall => Palette.Wall,
                        Cell.Exit => Palette.Exit,
                        Cell.MonsterSpawn => Palette.Spawn,
                        _ => Palette.Floor,
                    };
                    _tiles[p] = MakeSprite($"tile {x},{y}", ProceduralSprites.Diamond(color, cell.ToString()), p, 0);
                    _fog[p] = MakeSprite($"fog {x},{y}", ProceduralSprites.Diamond(Color.white, "fog"), p, 1, Palette.Fog);
                    _highlights[p] = MakeSprite($"hl {x},{y}", ProceduralSprites.Diamond(Color.white, "hl"), p, 1, Color.clear);
                }

            Refresh(state);
            FitCamera(map);
        }

        /// <summary>Sincroniza sprites com o estado (posições, baús, névoa, cartas no chão).</summary>
        public void Refresh(GameState state)
        {
            var view = HunterView.For(state, _viewerId, _content);
            var visible = new HashSet<Position>(view.VisibleCells);

            foreach (var (p, fog) in _fog)
                fog.color = visible.Contains(p) ? Color.clear : Palette.Fog;

            foreach (var chest in state.Chests)
            {
                if (!_chests.TryGetValue(chest.Position, out var sr))
                    _chests[chest.Position] = sr = MakeSprite($"chest {chest.Position}", ProceduralSprites.Shape(Palette.ChestClosed, false, "chest"), chest.Position, 3);
                sr.sprite = ProceduralSprites.Shape(chest.IsOpened ? Palette.ChestOpened : Palette.ChestClosed, false, chest.IsOpened ? "chest_open" : "chest");
            }

            foreach (var g in _ground.Values)
                g.gameObject.SetActive(false);
            foreach (var pile in view.GroundCards.GroupBy(g => g.Position))
            {
                if (!_ground.TryGetValue(pile.Key, out var sr))
                    _ground[pile.Key] = sr = MakeSprite($"ground {pile.Key}", ProceduralSprites.Disc(Palette.Ground, "ground"), pile.Key, 2);
                sr.gameObject.SetActive(true);
                sr.transform.localScale = Vector3.one * 0.5f;
            }

            foreach (var hunter in state.Hunters)
            {
                var other = view.Others.FirstOrDefault(o => o.Id == hunter.Id);
                var position = hunter.Id == _viewerId ? hunter.Position : other?.Position;
                var shown = hunter.IsActive && position != null;

                if (!_hunters.TryGetValue(hunter.Id, out var sr))
                {
                    var color = Palette.Hunters[(hunter.Id - 1) % Palette.Hunters.Length];
                    _hunters[hunter.Id] = sr = MakeSprite($"hunter {hunter.Id}", ProceduralSprites.Disc(color, "h" + hunter.Id), hunter.Position, 4);
                    _hunterMarks[hunter.Id] = MakeSprite($"mark {hunter.Id}", ProceduralSprites.Disc(Palette.Marked, "mark"), hunter.Position, 4);
                    _hunterMarks[hunter.Id].transform.localScale = Vector3.one * 1.3f;
                }

                sr.gameObject.SetActive(shown);
                _hunterMarks[hunter.Id].gameObject.SetActive(shown && state.IsMarked(hunter.Id));
                if (shown && !_moving.Contains(sr))
                {
                    Place(sr, position, 4);
                    Place(_hunterMarks[hunter.Id], position, 3);
                }
            }

            var visibleMonsters = view.Monsters.ToDictionary(m => m.Id);
            foreach (var kv in _monsters.ToList())
            {
                if (!visibleMonsters.ContainsKey(kv.Key))
                    kv.Value.gameObject.SetActive(false);
            }

            foreach (var monster in visibleMonsters.Values)
            {
                if (!_monsters.TryGetValue(monster.Id, out var sr))
                    _monsters[monster.Id] = sr = MakeSprite($"monster {monster.Id}", ProceduralSprites.Shape(Palette.Monster(monster.TypeId), true, monster.TypeId), monster.Position, 4);
                sr.gameObject.SetActive(true);
                if (!_moving.Contains(sr))
                    Place(sr, monster.Position, 4);
                sr.transform.localScale = monster.TypeId == MonsterCatalog.BossId ? Vector3.one * 1.8f : Vector3.one;
            }
        }

        public void Highlight(IEnumerable<Position> reachable, IEnumerable<Position> targets)
        {
            foreach (var hl in _highlights.Values)
                hl.color = Color.clear;
            foreach (var p in reachable)
                if (_highlights.TryGetValue(p, out var hl))
                    hl.color = Palette.Reachable;
            foreach (var p in targets)
                if (_highlights.TryGetValue(p, out var hl))
                    hl.color = Palette.Target;
        }

        private readonly HashSet<SpriteRenderer> _moving = new();

        /// <summary>Anima um evento; devolve quando a animação terminou.</summary>
        public IEnumerator Animate(GameEvent e)
        {
            switch (e)
            {
                case HunterMoved m when _hunters.TryGetValue(m.HunterId, out var sr) && sr.gameObject.activeSelf:
                    yield return Slide(sr, m.From, m.To, 4);
                    if (_hunterMarks.TryGetValue(m.HunterId, out var mark))
                        Place(mark, m.To, 3);
                    break;
                case MonsterMoved m when _monsters.TryGetValue(m.MonsterId, out var sr) && sr.gameObject.activeSelf:
                    yield return Slide(sr, m.From, m.To, 4);
                    break;
                case AttackResolved a:
                    yield return Floating(TargetPosition(a.Target), a.Dodged ? "esquivou" : a.Critical ? $"-{a.Damage}!" : $"-{a.Damage}", a.Dodged ? Color.cyan : Color.red);
                    break;
                case DamageDealt d:
                    yield return Floating(TargetPosition(d.Target), $"-{d.Damage}", new Color(1f, 0.5f, 0.2f));
                    break;
                case HunterHealed h:
                    yield return Floating(HunterPosition(h.HunterId), $"+{h.Amount}", Color.green);
                    break;
                case CardStolen s:
                    yield return Floating(HunterPosition(s.VictimId), "roubo!", Color.yellow);
                    break;
                case HunterFell f:
                    yield return Floating(f.Position, "caiu", Color.gray);
                    break;
                case BossBreath b:
                    foreach (var cell in b.Cells)
                        if (_highlights.TryGetValue(cell, out var hl))
                            hl.color = Palette.Target;
                    yield return new WaitForSeconds(0.3f);
                    foreach (var cell in b.Cells)
                        if (_highlights.TryGetValue(cell, out var hl))
                            hl.color = Color.clear;
                    break;
            }
        }

        private Position TargetPosition(Combatant c) => c.Kind == CombatantKind.Monster
            ? (_monsters.TryGetValue(c.Id, out var m) ? Iso.ToCell(m.transform.position) : null)
            : HunterPosition(c.Id);

        private Position HunterPosition(int id) => _hunters.TryGetValue(id, out var h) ? Iso.ToCell(h.transform.position) : null;

        private IEnumerator Slide(SpriteRenderer sr, Position from, Position to, int layer)
        {
            _moving.Add(sr);
            var a = Iso.ToWorld(from);
            var b = Iso.ToWorld(to);
            var seconds = GameSession.AiDelay > 0f ? MoveSeconds : 0f;
            for (var t = 0f; seconds > 0f && t < seconds; t += Time.deltaTime)
            {
                sr.transform.position = Vector3.Lerp(a, b, t / seconds);
                yield return null;
            }
            Place(sr, to, layer);
            _moving.Remove(sr);
        }

        private IEnumerator Floating(Position at, string text, Color color)
        {
            if (at == null || GameSession.AiDelay <= 0f)
                yield break;

            var go = new GameObject("float");
            go.transform.SetParent(_root, false);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.color = color;
            tm.font = _font;
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            go.GetComponent<MeshRenderer>().sortingOrder = 1000;
            var start = Iso.ToWorld(at) + Vector3.up * 0.5f;
            for (var t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                go.transform.position = start + Vector3.up * t;
                yield return null;
            }
            Destroy(go);
        }

        private SpriteRenderer MakeSprite(string name, Sprite sprite, Position at, int layer, Color? tint = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (tint != null)
                sr.color = tint.Value;
            Place(sr, at, layer);
            return sr;
        }

        private static void Place(SpriteRenderer sr, Position at, int layer)
        {
            sr.transform.position = Iso.ToWorld(at);
            sr.sortingOrder = Iso.SortingOrder(at, layer);
        }

        private static void FitCamera(GridMap map)
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var center = Iso.Center(map.Width, map.Height);
            cam.transform.position = new Vector3(center.x, center.y + 0.6f, -10f);
            var halfWidth = (map.Width + map.Height) * Iso.CellWidth * 0.25f + 0.5f;
            var halfHeight = (map.Width + map.Height) * Iso.CellHeight * 0.25f + 2.2f;
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
        }
    }
}
