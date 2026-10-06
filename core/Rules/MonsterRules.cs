using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>
/// Monstros (GDD): fase ao fim de cada rodada — cada monstro rola 1d6 de movimento e persegue o caçador
/// mais próximo em linha de visão; se adjacente, ataca em vez de mover. Spawn inicial e periódico.
/// A máquina de estados por tipo entra na fatia 4.
/// </summary>
internal static class MonsterRules
{
    /// <summary>Fase dos monstros, em ordem de id. Monstros que nascem durante a fase não agem nela.</summary>
    public static GameState Phase(GameState state, IRandom random, GameContent content, List<GameEvent> events)
    {
        foreach (var id in state.Monsters.Select(m => m.Id).OrderBy(i => i).ToList())
        {
            if (state.Monsters.All(m => m.Id != id))
                continue;

            if (!state.Hunters.Any(h => h.IsActive))
                break;

            state = Act(state, id, random, content, events);
        }

        events.Add(new MonsterPhaseEnded(state.Round));
        return state;
    }

    private static GameState Act(GameState state, int monsterId, IRandom random, GameContent content, List<GameEvent> events)
    {
        var monster = state.Monster(monsterId);
        var target = NearestVisibleHunter(state, monster);
        if (target == null)
            return state;

        if (monster.Position.IsOrthogonallyAdjacentTo(target.Position))
            return CombatRules.ResolveAttack(state, Combatant.MonsterRef(monsterId), Combatant.HunterRef(target.Id), random, content, events);

        var steps = random.NextD6();
        var from = monster.Position;
        var position = from;

        // Caminho até o caçador: o alvo em si conta como destino, as outras criaturas bloqueiam.
        var distances = Pathfinding.Distances(state.Map, target.Position, p => state.Map.IsWalkable(p) && !state.IsOccupied(p) || p == position);

        for (var i = 0; i < steps && !position.IsOrthogonallyAdjacentTo(target.Position); i++)
        {
            var next = Pathfinding.Neighbors(position)
                .Where(n => distances.ContainsKey(n) && state.Map.IsWalkable(n) && !state.IsOccupied(n))
                .OrderBy(n => distances[n])
                .ThenBy(n => n.Y).ThenBy(n => n.X)
                .FirstOrDefault();

            if (next == null || distances[next] >= distances[position])
                break;

            position = next;
            state = state.WithMonster(monster with { Position = position });
        }

        if (position != from)
            events.Add(new MonsterMoved(monsterId, from, position));

        return state;
    }

    private static Hunter? NearestVisibleHunter(GameState state, Monster monster) =>
        state.Hunters
            .Where(h => h.IsActive && LineOfSight.IsClear(state.Map, monster.Position, h.Position))
            .OrderBy(h => h.Position.DistanceTo(monster.Position))
            .ThenBy(h => h.Id)
            .FirstOrDefault();

    /// <summary>Monstro derrotado: some do mapa, dá XP ao caçador e, com 1d6 ≤ SOR, uma carta da sua tabela de loot.</summary>
    public static GameState Defeat(GameState state, int monsterId, Combatant killedBy, IRandom random, GameContent content, List<GameEvent> events)
    {
        var monster = state.Monster(monsterId);
        var type = content.Monsters.Get(monster.TypeId);
        state = state.WithMonsterRemoved(monsterId);

        if (killedBy.Kind != CombatantKind.Hunter)
            return state;

        var killer = state.Hunter(killedBy.Id);
        state = state.WithHunter(killer with { Xp = killer.Xp + type.Xp });
        events.Add(new MonsterDefeated(monsterId, type.Id, killer.Id, type.Xp));

        var luck = StatRules.Effective(killer, content).Luck;
        if (random.NextD6() > luck)
            return state;

        var card = LootRoller.Roll(content.LootTable(type.Loot), content.Cards, luck, random);
        return ChestRules.GiveCard(state, killer.Id, card.Id, events);
    }

    /// <summary>Spawn inicial: um monstro sorteado em cada spawn (uma por sala sem caçador).</summary>
    public static GameState SpawnInitial(GameState state, IRandom random, GameContent content, List<GameEvent> events)
    {
        foreach (var spawn in state.MonsterSpawns)
            state = SpawnAt(state, spawn, random, content, events);

        return state;
    }

    /// <summary>A cada MonsterSpawnInterval rodadas, +1 monstro num spawn aleatório livre.</summary>
    public static GameState SpawnIfDue(GameState state, IRandom random, GameContent content, List<GameEvent> events)
    {
        var interval = state.Config.MonsterSpawnInterval;
        if (interval <= 0 || state.Round <= 1 || (state.Round - 1) % interval != 0)
            return state;

        var free = state.MonsterSpawns.Where(p => !state.IsOccupied(p)).ToList();
        if (free.Count == 0)
            return state;

        return SpawnAt(state, free[random.Next(free.Count)], random, content, events);
    }

    public static GameState SpawnAt(GameState state, Position position, IRandom random, GameContent content, List<GameEvent> events)
    {
        var types = content.Monsters.Spawnable;
        var type = types[random.Next(types.Count)];
        return Spawn(state, type, position, events);
    }

    public static GameState Spawn(GameState state, MonsterType type, Position position, List<GameEvent> events)
    {
        var monster = new Monster(state.NextMonsterId, type.Id, type.Hp, position);
        events.Add(new MonsterSpawned(monster.Id, type.Id, position));
        return state.WithMonsterAdded(monster);
    }
}
