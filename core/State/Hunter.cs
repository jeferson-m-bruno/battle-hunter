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
    Equipment Equipment)
{
    public static Hunter Create(
        int id,
        string name,
        HunterStats stats,
        Position position,
        IReadOnlyList<string>? hand = null,
        Equipment? equipment = null) =>
        new(id, name, stats, stats.MaxHp, position, HunterStatus.Active, hand ?? Array.Empty<string>(), equipment ?? Equipment.None);

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
}
