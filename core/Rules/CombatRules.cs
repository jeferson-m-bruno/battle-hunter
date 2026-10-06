using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>
/// Fórmula de dano do GDD, usada por caçadores e monstros:
/// dano = max(1, (ATQ + 1d6) − (DEF + 1d6 do alvo)); crítico dobra, esquiva zera.
/// </summary>
internal static class CombatRules
{
    public const int AttackCost = 2;
    public const int XpPerLevelOnPvpKill = 10;

    public static ReducerResult Attack(GameState state, Attack action, IRandom random, GameContent content)
    {
        var error = Reducer.CheckTurn(state, action, GamePhase.Acting);
        if (error != null)
            return Reducer.Reject(state, action, error);

        if (state.ActionPoints < AttackCost)
            return Reducer.Reject(state, action, "PA insuficiente para atacar.");

        var hunter = state.Hunter(action.HunterId);
        if (!hunter.Position.IsOrthogonallyAdjacentTo(action.Target))
            return Reducer.Reject(state, action, "O alvo precisa estar adjacente.");

        var target = TargetAt(state, action.Target);
        if (target == null)
            return Reducer.Reject(state, action, "Não há alvo nessa célula.");

        var pointsLeft = state.ActionPoints - AttackCost;
        var next = state with { ActionPoints = pointsLeft };
        var events = new List<GameEvent>();

        next = ResolveAttack(next, Combatant.HunterRef(hunter.Id), target, random, content, events);

        if (next.Phase == GamePhase.Finished)
            return new ReducerResult(next, events);

        if (pointsLeft == 0)
            next = TurnRules.EndTurn(next, random, content, events);

        return new ReducerResult(next, events);
    }

    public static Combatant? TargetAt(GameState state, Position position)
    {
        var monster = state.MonsterAt(position);
        if (monster != null)
            return Combatant.MonsterRef(monster.Id);

        var hunter = state.HunterAt(position);
        return hunter != null ? Combatant.HunterRef(hunter.Id) : null;
    }

    /// <summary>Rola os dados, aplica o dano e resolve morte/queda. Serve para caçador ou monstro atacante.</summary>
    public static GameState ResolveAttack(GameState state, Combatant attacker, Combatant target, IRandom random, GameContent content, List<GameEvent> events)
    {
        var (atk, atkLuck) = OffensiveStats(state, attacker, content);
        var (def, defLuck) = DefensiveStats(state, target, content);

        var attackerDie = random.NextD6();
        var defenderDie = random.NextD6();
        var damage = Math.Max(1, atk + attackerDie - (def + defenderDie));
        var critical = false;
        var dodged = false;

        if (attackerDie == 6 && random.NextD6() <= atkLuck)
            critical = true;

        if (defenderDie == 6 && random.NextD6() <= defLuck)
            dodged = true;

        if (dodged)
            damage = 0;
        else if (critical)
            damage *= 2;

        state = ApplyDamage(state, attacker, target, damage, random, content, events,
            hpLeft => new AttackResolved(attacker, target, attackerDie, defenderDie, damage, critical, dodged, hpLeft));

        // PvP: após dano, teste de roubo (GDD, seção de cartas).
        if (damage > 0 && attacker.Kind == CombatantKind.Hunter && target.Kind == CombatantKind.Hunter)
            state = StealRules.TryStealAfterHit(state, attacker.Id, target.Id, random, content, events);

        return state;
    }

    /// <summary>Aplica dano já calculado (ataque, sopro, bomba, armadilha) e resolve morte/queda.</summary>
    public static GameState ApplyDamage(
        GameState state,
        Combatant source,
        Combatant target,
        int damage,
        IRandom random,
        GameContent content,
        List<GameEvent> events,
        Func<int, GameEvent> makeEvent)
    {
        if (target.Kind == CombatantKind.Monster)
        {
            var monster = state.Monster(target.Id);
            var hpLeft = Math.Max(0, monster.Hp - damage);
            events.Add(makeEvent(hpLeft));
            state = state.WithMonster(monster with { Hp = hpLeft });
            return hpLeft == 0 ? MonsterRules.Defeat(state, monster.Id, source, random, content, events) : state;
        }
        else
        {
            var hunter = state.Hunter(target.Id);
            var hpLeft = Math.Max(0, hunter.Hp - damage);
            events.Add(makeEvent(hpLeft));
            state = state.WithHunter(hunter with { Hp = hpLeft });
            return hpLeft == 0 ? Fall(state, hunter.Id, source, events) : state;
        }
    }

    /// <summary>Caçador a 0 PV: cai, larga toda a mão na célula; quem derrubou (se caçador) ganha nível × 10 de XP.</summary>
    public static GameState Fall(GameState state, int hunterId, Combatant killedBy, List<GameEvent> events)
    {
        var hunter = state.Hunter(hunterId);
        var dropped = hunter.Hand.ToList();

        state = state
            .WithHunter(hunter with { Status = HunterStatus.Fallen, Hand = Array.Empty<string>() })
            .WithGroundCards(dropped.Select(c => new GroundCard(hunter.Position, c)));

        if (killedBy.Kind == CombatantKind.Hunter)
        {
            var killer = state.Hunter(killedBy.Id);
            state = state.WithHunter(killer with { Xp = killer.Xp + hunter.Level * XpPerLevelOnPvpKill });
        }

        events.Add(new HunterFell(hunterId, killedBy, hunter.Position, dropped));
        return state;
    }

    private static (int Attack, int Luck) OffensiveStats(GameState state, Combatant c, GameContent content)
    {
        if (c.Kind == CombatantKind.Monster)
        {
            var type = content.Monsters.Get(state.Monster(c.Id).TypeId);
            return (type.Atk, 0);
        }

        var stats = StatRules.Effective(state.Hunter(c.Id), content);
        return (stats.Attack, stats.Luck);
    }

    private static (int Defense, int Luck) DefensiveStats(GameState state, Combatant c, GameContent content)
    {
        if (c.Kind == CombatantKind.Monster)
        {
            var type = content.Monsters.Get(state.Monster(c.Id).TypeId);
            return (type.Def, 0);
        }

        var stats = StatRules.Effective(state.Hunter(c.Id), content);
        return (stats.Defense, stats.Luck);
    }
}
