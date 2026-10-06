using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>
/// Chefe (GDD): aparece na rodada configurada ou quando o tesouro-alvo é pego, na sala da saída.
/// Sopro: a cada 3 ações, linha de 3 células na direção do caçador mais próximo, 8 de dano fixo,
/// ignora DEF e esquiva. Derrotá-lo dá 150 XP e uma carta rara garantida.
/// </summary>
internal static class BossRules
{
    public const int BreathDamage = 8;
    public const int BreathLength = 3;
    public const int BreathEvery = 3;

    public static bool IsEnabled(GameState state) => state.Config.BossRound > 0;

    /// <summary>Spawn por rodada: chamado quando uma rodada nova começa.</summary>
    public static GameState SpawnIfDue(GameState state, IRandom random, GameContent content, List<GameEvent> events) =>
        IsEnabled(state) && !state.BossSpawned && state.Round >= state.Config.BossRound
            ? Spawn(state, random, content, events)
            : state;

    /// <summary>Spawn por tesouro: chamado quando o tesouro-alvo entra na mão de alguém.</summary>
    public static GameState SpawnOnTreasure(GameState state, IRandom random, GameContent content, List<GameEvent> events) =>
        IsEnabled(state) && !state.BossSpawned
            ? Spawn(state, random, content, events)
            : state;

    private static GameState Spawn(GameState state, IRandom random, GameContent content, List<GameEvent> events)
    {
        var type = content.Monsters.Get(MonsterCatalog.BossId);
        var hp = type.Hp * state.Config.BossHpPercent / 100;
        var position = SpawnCell(state, random);
        if (position == null)
            return state;

        var boss = new Monster(state.NextMonsterId, type.Id, hp, position);
        events.Add(new MonsterSpawned(boss.Id, type.Id, position));
        events.Add(new BossAppeared(boss.Id, position));
        return state.WithMonsterAdded(boss) with { BossSpawned = true };
    }

    /// <summary>Célula livre na sala da saída (ou a até 2 passos da saída, em mapas sem salas).</summary>
    private static Position? SpawnCell(GameState state, IRandom random)
    {
        var exit = ExitCell(state.Map);
        if (exit == null)
            return null;

        var room = state.Map.RoomAt(exit);
        var area = room != null
            ? room.Cells()
            : Visibility.VisibleCells(state.Map, exit);

        var free = area.Where(p => state.Map.IsWalkable(p) && !state.IsOccupied(p)).OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
        return free.Count == 0 ? null : free[random.Next(free.Count)];
    }

    private static Position? ExitCell(GridMap map)
    {
        for (var y = 0; y < map.Height; y++)
            for (var x = 0; x < map.Width; x++)
            {
                var p = new Position(x, y);
                if (map[p] == Cell.Exit)
                    return p;
            }

        return null;
    }

    /// <summary>Sopro na direção ortogonal do caçador ativo mais próximo; para em paredes e na borda.</summary>
    public static GameState Breath(GameState state, int bossId, IRandom random, GameContent content, List<GameEvent> events)
    {
        var boss = state.Monster(bossId);
        var target = state.Hunters.Where(h => h.IsActive).OrderBy(h => h.Position.DistanceTo(boss.Position)).ThenBy(h => h.Id).FirstOrDefault();
        if (target == null)
            return state;

        var dx = target.Position.X - boss.Position.X;
        var dy = target.Position.Y - boss.Position.Y;
        var (stepX, stepY) = System.Math.Abs(dx) >= System.Math.Abs(dy)
            ? (System.Math.Sign(dx), 0)
            : (0, System.Math.Sign(dy));

        var cells = new List<Position>();
        var p = boss.Position;
        for (var i = 0; i < BreathLength; i++)
        {
            p = new Position(p.X + stepX, p.Y + stepY);
            if (!state.Map.IsInside(p) || state.Map[p] == Cell.Wall)
                break;
            cells.Add(p);
        }

        events.Add(new BossBreath(bossId, cells));
        var source = Combatant.MonsterRef(bossId);

        foreach (var hunter in state.Hunters.Where(h => h.IsActive && cells.Contains(h.Position)).ToList())
        {
            var victim = Combatant.HunterRef(hunter.Id);
            state = CombatRules.ApplyDamage(state, source, victim, BreathDamage, random, content, events,
                hpLeft => new DamageDealt(source, victim, BreathDamage, hpLeft, "breath"));
        }

        return state;
    }
}
