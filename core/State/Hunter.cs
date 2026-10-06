using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleHunter.Core.State;

public sealed record Hunter(
    int Id,
    string Name,
    HunterStats Stats,
    int Hp,
    Position Position,
    HunterStatus Status,
    IReadOnlyList<string> Hand,
    Equipment Equipment,
    int Level,
    int Xp,
    IReadOnlyList<StatusEffect> Statuses)
{
    public static Hunter Create(
        int id,
        string name,
        HunterStats stats,
        Position position,
        IReadOnlyList<string>? hand = null,
        Equipment? equipment = null,
        int level = 1) =>
        new(id, name, stats, stats.MaxHp, position, HunterStatus.Active, hand ?? Array.Empty<string>(), equipment ?? Equipment.None, level, Xp: 0, Array.Empty<StatusEffect>());

    public bool IsActive => Status == HunterStatus.Active;

    public bool HasCard(string cardId) => Hand.Contains(cardId, StringComparer.Ordinal);

    public Hunter WithCardAdded(string cardId) => this with { Hand = Hand.Append(cardId).ToList() };

    /// <summary>Remove uma ocorrência da carta; ignora se não está na mão.</summary>
    public Hunter WithCardRemoved(string cardId)
    {
        var hand = Hand.ToList();
        hand.Remove(cardId);
        return this with { Hand = hand };
    }

    public bool HasStatus(StatusKind kind) => Statuses.Any(s => s.Kind == kind);

    /// <summary>Aplica ou renova um estado (fica a maior duração).</summary>
    public Hunter WithStatus(StatusKind kind, int turns)
    {
        var others = Statuses.Where(s => s.Kind != kind).ToList();
        var current = Statuses.FirstOrDefault(s => s.Kind == kind);
        others.Add(new StatusEffect(kind, Math.Max(turns, current?.TurnsLeft ?? 0)));
        return this with { Statuses = others };
    }

    public Hunter WithoutStatus(StatusKind kind) => this with { Statuses = Statuses.Where(s => s.Kind != kind).ToList() };
}
