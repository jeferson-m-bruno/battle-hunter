using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Ai;

/// <summary>Outro caçador como visto por este: posição só se visível ou Marcado; equipamento sempre.</summary>
public sealed record SeenHunter(int Id, Position? Position, int? Hp, Equipment Equipment, bool IsMarked, HunterStatus Status);

/// <summary>
/// O que um jogador (humano ou IA) sabe da partida: o layout do mapa, a própria mão e armadilhas,
/// o equipamento de todos, o Marcado sempre, e criaturas, cartas no chão e baús apenas nas células visíveis
/// (sala atual + 2 passos). Baús abertos e a presença do chefe são públicos.
/// </summary>
public sealed class HunterView
{
    private HunterView(GameState state, int hunterId, GameContent content)
    {
        Self = state.Hunter(hunterId);
        Stats = StatRules.Effective(Self, content);
        var visible = new HashSet<Position>(Visibility.VisibleCells(state.Map, Self.Position));
        VisibleCells = visible;

        Others = state.Hunters.Where(h => h.Id != hunterId).Select(h =>
        {
            var marked = state.IsMarked(h.Id);
            var seen = h.IsActive && (marked || visible.Contains(h.Position));
            return new SeenHunter(h.Id, seen ? h.Position : null, seen ? h.Hp : null, h.Equipment, marked, h.Status);
        }).ToList();

        Monsters = state.Monsters.Where(m => visible.Contains(m.Position)).ToList();
        GroundCards = state.GroundCards.Where(g => visible.Contains(g.Position)).ToList();
        OwnTraps = state.Traps.Where(t => t.OwnerId == hunterId).ToList();
        Chests = state.Chests.Select(c => c with { IsMimic = false }).ToList();
        BossPresent = state.Monsters.Any(m => m.TypeId == MonsterCatalog.BossId);
        BossPosition = Monsters.FirstOrDefault(m => m.TypeId == MonsterCatalog.BossId)?.Position;
        IsMarked = state.IsMarked(hunterId);
    }

    public Hunter Self { get; }
    public HunterStats Stats { get; }
    public IReadOnlyCollection<Position> VisibleCells { get; }
    public IReadOnlyList<SeenHunter> Others { get; }
    public IReadOnlyList<Monster> Monsters { get; }
    public IReadOnlyList<GroundCard> GroundCards { get; }
    public IReadOnlyList<Trap> OwnTraps { get; }
    /// <summary>Todos os baús (o layout é público); o segredo do Mímico não vaza.</summary>
    public IReadOnlyList<Chest> Chests { get; }
    public bool BossPresent { get; }
    public Position? BossPosition { get; }
    public bool IsMarked { get; }

    public int HpPercent => Self.Hp * 100 / System.Math.Max(1, Stats.MaxHp);

    public static HunterView For(GameState state, int hunterId, GameContent content) => new(state, hunterId, content);
}
