using System.Collections;
using System.Linq;
using BattleHunter.Client.UI;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BattleHunter.Client.Presentation
{
    /// <summary>HUD do topo: PV, PA, rodada, ordem de turno, timer, dado animado, mensagem; botões Passar/Sair.</summary>
    public sealed class HudView : MonoBehaviour
    {
        private IGameHost _host;
        private Text _status;
        private Text _order;
        private Text _dice;
        private Text _message;
        private Text _timer;
        private Button _pass;
        private Button _exit;
        private Coroutine _diceAnim;
        private float _messageUntil;

        public void Build(Transform canvas, IGameHost host)
        {
            _host = host;
            var top = Ui.PanelRect(canvas, "Hud", new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            _status = Ui.Label(top, "Status", "", 34, TextAnchor.UpperLeft, new Vector2(0f, 0.45f), new Vector2(0.72f, 1f), new Vector2(24, 0), new Vector2(0, -16));
            _order = Ui.Label(top, "Order", "", 26, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0.72f, 0.5f), new Vector2(24, 12), Vector2.zero, new Color(0.85f, 0.85f, 0.85f));
            _dice = Ui.Label(top, "Dice", "", 72, TextAnchor.MiddleCenter, new Vector2(0.72f, 0.35f), new Vector2(1f, 1f), color: Ui.Accent);
            _timer = Ui.Label(top, "Timer", "", 26, TextAnchor.LowerCenter, new Vector2(0.72f, 0f), new Vector2(1f, 0.4f), new Vector2(0, 12));

            var bar = Ui.PanelRect(canvas, "Actions", new Vector2(0f, 0.80f), new Vector2(1f, 0.86f), new Color(0, 0, 0, 0.35f));
            _pass = Ui.Button(bar, "Pass", "Passar", new Vector2(0.52f, 0.1f), new Vector2(0.74f, 0.9f), () => _host.Submit(new Pass(_host.HumanId)), fontSize: 30);
            _exit = Ui.Button(bar, "Exit", "Sair", new Vector2(0.76f, 0.1f), new Vector2(0.98f, 0.9f), () => _host.Submit(new Exit(_host.HumanId)), fontSize: 30, color: new Color(0.2f, 0.5f, 0.3f));
            _message = Ui.Label(bar, "Message", "", 26, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(24, 0), Vector2.zero, Ui.Accent);

            host.OnStateChanged += Refresh;
            host.OnEvent += OnEvent;
            Refresh();
        }

        private void Update()
        {
            if (_host == null || _host.View == null)
                return;

            _timer.text = _host.IsHumanTurn ? $"{Mathf.CeilToInt(_host.TurnTimeLeft)} s" : "";
            if (_message != null && Time.time > _messageUntil)
                _message.text = "";
        }

        public void Show(string text, float seconds = 2.5f)
        {
            _message.text = text;
            _messageUntil = Time.time + seconds;
        }

        private void Refresh()
        {
            var v = _host.View;
            if (v == null)
                return;

            var me = v.Self;
            var stats = v.EffectiveStats;
            var marked = v.IsMarked ? "  ★ tesouro" : "";
            var statuses = me.Statuses.Count > 0 ? "  " + string.Join(" ", me.Statuses.Select(s => s.Kind == StatusKind.Poisoned ? "☠" : "⛓")) : "";
            _status.text = $"{me.Name}  PV {me.Hp}/{stats.MaxHp}  ATQ {stats.Attack}  DEF {stats.Defense}  SOR {stats.Luck}{marked}{statuses}\nRodada {v.Round}/{v.MaxRounds}   PA {v.ActionPoints}   {_host.Title}";

            if (v.TurnOrder.Count > 0)
            {
                _order.text = "Ordem: " + string.Join("  ", v.TurnOrder.Select(id =>
                {
                    var h = v.Hunter(id);
                    var tag = h.Status == HunterStatus.Fallen ? "✕" : h.Status == HunterStatus.Exited ? "→" : h.IsMarked ? "★" : "";
                    var name = id == _host.HumanId ? "Você" : h.Name.Replace("IA ", "");
                    return !v.IsFinished() && id == v.CurrentHunterId ? $"[{name}{tag}]" : $"{name}{tag}";
                }));
            }

            var human = _host.IsHumanTurn;
            _pass.gameObject.SetActive(human && v.Phase == GamePhase.Acting);
            _exit.gameObject.SetActive(human && v.Phase == GamePhase.Acting && v.Map[me.Position] == Cell.Exit && v.ActionPoints >= 1);

            if (v.Phase == GamePhase.AwaitingRoll && _diceAnim == null)
                _dice.text = human ? "toque\nno dado" : "…";
        }

        private void OnEvent(GameEvent e)
        {
            var v = _host.View;
            switch (e)
            {
                case DiceRolled d:
                    if (_diceAnim != null)
                        StopCoroutine(_diceAnim);
                    _diceAnim = StartCoroutine(RollDice(d));
                    break;
                case ActionRejected r:
                    Show(r.Reason);
                    break;
                case HunterMarked m:
                    Show(m.HunterId == _host.HumanId ? "Você pegou o tesouro! Corra para a saída." : $"{v?.NameOf(m.HunterId)} pegou o tesouro!");
                    break;
                case BossAppeared:
                    Show("O Dragão apareceu na sala da saída!", 4f);
                    break;
                case TurnStarted t when t.HunterId == _host.HumanId:
                    Show("Sua vez: toque no dado.");
                    break;
                case GameEnded g:
                    Show(g.Reason switch
                    {
                        GameEndReason.TreasureExtracted => $"{v?.NameOf(g.WinnerId ?? 0)} venceu a missão!",
                        GameEndReason.RoundLimit => "Limite de rodadas: missão falhou.",
                        _ => "Todos os caçadores caíram.",
                    }, 5f);
                    break;
            }
        }

        private IEnumerator RollDice(DiceRolled d)
        {
            var seconds = GameSession.AiDelay > 0f ? 0.5f : 0f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                _dice.text = Random.Range(1, 7).ToString();
                yield return null;
            }

            _dice.text = d.SpeedBonus != 0 ? $"{d.Die}{(d.SpeedBonus > 0 ? "+" : "")}{d.SpeedBonus}" : d.Die.ToString();
            _diceAnim = null;
        }
    }
}
