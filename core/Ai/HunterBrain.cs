using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;

namespace BattleHunter.Core.Ai;

/// <summary>
/// IA de caçador por utilidade (GDD, seção IA): a cada chamada lista as ações possíveis com o PA atual,
/// pontua cada situação pelos pesos de data/ai_weights.json (ajustados pelo perfil) e devolve a melhor.
/// O host chama de novo até o turno acabar. Só enxerga o que um jogador veria (HunterView).
/// </summary>
public static class HunterBrain
{
    private static readonly string[] AdjacentSpecialEffects = { "strike", "double_strike", "trip", "life_steal", "whirlwind" };

    public static GameAction Decide(GameState state, int hunterId, AiProfile profile, AiMemory memory, GameContent content)
    {
        if (state.Phase == GamePhase.AwaitingRoll)
            return new RollDice(hunterId);

        var view = HunterView.For(state, hunterId, content);
        memory.Observe(view);
        var ctx = new Context(state, view, profile, memory, content);

        var candidates = new List<(int Score, GameAction Action)>();
        void Add(string key, GameAction? action)
        {
            if (action != null)
                candidates.Add((content.Ai.Weight(profile, key), action));
        }

        Add("treasure_to_exit", TreasureToExit(ctx));
        Add("hunt_marked", HuntMarked(ctx));
        Add("heal", Heal(ctx));
        Add("cure", Cure(ctx));
        Add("flee", Flee(ctx));
        Add("open_chest", OpenChest(ctx));
        Add("attack_monster", AttackMonster(ctx));
        Add("equip", Equip(ctx));
        Add("avoid_boss", AvoidBoss(ctx));
        Add("explore", Explore(ctx));

        return candidates.Count == 0
            ? new Pass(hunterId)
            : candidates.OrderByDescending(c => c.Score).First().Action;
    }

    private sealed record Context(GameState State, HunterView View, AiProfile Profile, AiMemory Memory, GameContent Content)
    {
        public int Id => View.Self.Id;
        public Position Pos => View.Self.Position;
        public int Pa => State.ActionPoints;
        public int Threshold(string key, int fallback) => Content.Ai.Threshold(key, fallback);
    }

    // ---- situações -------------------------------------------------------

    private static GameAction? TreasureToExit(Context c)
    {
        if (!c.View.IsMarked)
            return null;

        var exit = ExitCell(c.State.Map);
        if (exit == null)
            return null;

        if (c.Pos == exit)
            return new Exit(c.Id);

        return StepToward(c, exit, stopAdjacent: false);
    }

    private static GameAction? HuntMarked(Context c)
    {
        if (c.View.HpPercent <= c.Threshold("hunt_marked_min_hp_percent", 50))
            return null;

        var carrier = c.View.Others.FirstOrDefault(o => o.IsMarked && o.Position != null && o.Status == HunterStatus.Active);
        if (carrier == null)
            return null;

        var target = carrier.Position!;
        if (c.Pos.IsOrthogonallyAdjacentTo(target))
            return c.Pa >= 2 ? BestAttack(c, target) : null;

        var path = PathTo(c, target, stopAdjacent: true);
        if (path == null || path.Count > c.Threshold("hunt_marked_max_steps", 3))
            return null;

        return new MoveTo(c.Id, path[0]);
    }

    private static GameAction? Heal(Context c)
    {
        if (c.View.HpPercent >= c.Threshold("heal_below_hp_percent", 30))
            return null;

        var heal = CardsInHand(c).Where(card => card.Effect == "heal").OrderByDescending(card => card.Param("amount")).FirstOrDefault();
        return heal != null && c.Pa >= 1 ? new UseCard(c.Id, heal.Id) : null;
    }

    private static GameAction? Cure(Context c)
    {
        if (!c.View.Self.HasStatus(StatusKind.Poisoned))
            return null;

        var cure = CardsInHand(c).FirstOrDefault(card => card.Effect == "cure_poison");
        return cure != null && c.Pa >= 1 ? new UseCard(c.Id, cure.Id) : null;
    }

    private static GameAction? Flee(Context c)
    {
        if (c.View.HpPercent > c.Threshold("flee_at_or_below_hp_percent", 40))
            return null;

        var adjacent = c.View.Monsters.Where(m => m.Position.IsOrthogonallyAdjacentTo(c.Pos)).ToList();
        if (adjacent.Count == 0)
            return null;

        return StepAwayFrom(c, adjacent.Select(m => m.Position).ToList());
    }

    private static GameAction? OpenChest(Context c)
    {
        var best = c.View.Chests
            .Where(ch => !ch.IsOpened)
            .Select(ch => (Chest: ch, Path: PathTo(c, ch.Position, stopAdjacent: true)))
            .Where(x => x.Path != null)
            .OrderBy(x => x.Path!.Count)
            .ThenBy(x => x.Chest.Position.Y).ThenBy(x => x.Chest.Position.X)
            .FirstOrDefault();

        if (best.Chest == null || best.Path!.Count > c.Threshold("chest_max_steps", 4))
            return null;

        if (c.Pos.IsOrthogonallyAdjacentTo(best.Chest.Position))
        {
            if (c.Pa < 2)
                return null;

            if (c.View.Self.Hand.Count >= GameState.MaxHandSize)
                return DiscardWorst(c);

            return new State.Actions.OpenChest(c.Id, best.Chest.Position);
        }

        return new MoveTo(c.Id, best.Path[0]);
    }

    private static GameAction? AttackMonster(Context c)
    {
        if (c.Pa < 2)
            return null;

        var adjacent = c.View.Monsters.Where(m => m.Position.IsOrthogonallyAdjacentTo(c.Pos)).OrderBy(m => m.Hp).FirstOrDefault();
        if (adjacent == null)
            return null;

        var isBoss = adjacent.TypeId == MonsterCatalog.BossId;
        var minHp = isBoss ? c.Threshold("boss_min_hp_percent", 60) : c.Threshold("flee_at_or_below_hp_percent", 40);
        if (c.View.HpPercent <= minHp)
            return null;

        return BestAttack(c, adjacent.Position);
    }

    private static GameAction? Equip(Context c)
    {
        if (c.Pa < 1)
            return null;

        var best = CardsInHand(c)
            .Where(card => card.IsEquipment)
            .Select(card => (Card: card, Gain: ModScore(card.Mods) - CurrentSlotScore(c, card)))
            .Where(x => x.Gain > 0)
            .OrderByDescending(x => x.Gain)
            .FirstOrDefault();

        return best.Card != null ? new State.Actions.Equip(c.Id, best.Card.Id) : null;
    }

    private static GameAction? AvoidBoss(Context c)
    {
        if (!c.View.BossPresent || c.View.IsMarked || c.View.BossPosition == null)
            return null;

        if (c.Pos.DistanceTo(c.View.BossPosition) > c.Threshold("avoid_boss_within_steps", 3))
            return null;

        return StepAwayFrom(c, new[] { c.View.BossPosition });
    }

    private static GameAction? Explore(Context c)
    {
        if (c.Pa < 1)
            return null;

        var distances = Pathfinding.Distances(c.State.Map, c.Pos, p => c.State.Map.IsWalkable(p) && !c.State.IsOccupied(p));
        var target = distances
            .Where(kv => kv.Value > 0 && !c.Memory.HasSeen(kv.Key))
            .OrderBy(kv => kv.Value).ThenBy(kv => kv.Key.Y).ThenBy(kv => kv.Key.X)
            .Select(kv => (Position?)kv.Key)
            .FirstOrDefault();

        if (target == null)
        {
            // Tudo visto: vai ao baú fechado mais perto, ou à saída.
            target = c.View.Chests.Where(ch => !ch.IsOpened)
                .Select(ch => (Position?)ch.Position)
                .OrderBy(p => Pathfinding.StepsTo(distances, p!) ?? int.MaxValue)
                .FirstOrDefault() ?? ExitCell(c.State.Map);
        }

        return target == null ? null : StepToward(c, target, stopAdjacent: !c.State.Map.IsWalkable(target));
    }

    // ---- utilidades --------------------------------------------------------

    private static GameAction? BestAttack(Context c, Position target)
    {
        var special = CardsInHand(c)
            .Where(card => card.Type == CardType.SpecialAttack && card.Effect != null && AdjacentSpecialEffects.Contains(card.Effect))
            .OrderByDescending(card => card.Sell)
            .FirstOrDefault();

        return special != null ? new UseCard(c.Id, special.Id, target) : new Attack(c.Id, target);
    }

    private static GameAction? DiscardWorst(Context c)
    {
        var worst = CardsInHand(c)
            .Where(card => card.Id != c.State.TargetTreasureCardId)
            .OrderBy(card => card.Sell)
            .FirstOrDefault();

        return worst != null ? new Discard(c.Id, worst.Id) : null;
    }

    private static GameAction? StepToward(Context c, Position target, bool stopAdjacent)
    {
        if (c.Pa < 1)
            return null;

        var path = PathTo(c, target, stopAdjacent);
        return path == null || path.Count == 0 ? null : new MoveTo(c.Id, path[0]);
    }

    private static GameAction? StepAwayFrom(Context c, IReadOnlyList<Position> threats)
    {
        if (c.Pa < 1)
            return null;

        var best = Pathfinding.Neighbors(c.Pos)
            .Where(n => c.State.Map.IsWalkable(n) && !c.State.IsOccupied(n))
            .Select(n => (Cell: n, Safety: threats.Min(t => t.DistanceTo(n))))
            .Where(x => x.Safety > 1)
            .OrderByDescending(x => x.Safety)
            .ThenBy(x => x.Cell.Y).ThenBy(x => x.Cell.X)
            .FirstOrDefault();

        return best.Cell == null ? null : new MoveTo(c.Id, best.Cell);
    }

    /// <summary>Caminho A* até o alvo (ou até uma célula adjacente a ele); criaturas bloqueiam.</summary>
    private static IReadOnlyList<Position>? PathTo(Context c, Position target, bool stopAdjacent)
    {
        bool Passable(Position p) => c.State.Map.IsWalkable(p) && !c.State.IsOccupied(p);

        var path = Pathfinding.AStar(c.State.Map, c.Pos, target, Passable);
        if (path == null)
            return null;

        if (stopAdjacent)
        {
            if (c.Pos.IsOrthogonallyAdjacentTo(target))
                return Array.Empty<Position>();
            path = path.Take(path.Count - 1).ToList();
        }

        return path.Count == 0 || Passable(path[0]) ? path : null;
    }

    private static IEnumerable<Card> CardsInHand(Context c) => c.View.Self.Hand.Select(id => c.Content.Cards.Get(id));

    private static int ModScore(StatMods m) => m.Atk * 2 + m.Def * 2 + m.Spd * 3 + m.Luck + m.Hp / 2;

    private static int CurrentSlotScore(Context c, Card card)
    {
        var current = c.View.Self.Equipment[Equipment.SlotFor(card.Type)];
        return current == null ? 0 : ModScore(c.Content.Cards.Get(current).Mods);
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
}
