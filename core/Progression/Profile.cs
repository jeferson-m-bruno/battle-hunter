using System;
using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Progression;

/// <summary>Pontos livres distribuídos pelo jogador (1 por nível).</summary>
public sealed record StatPoints(int Hp = 0, int Atk = 0, int Def = 0, int Spd = 0, int Luck = 0)
{
    public static StatPoints None => new();
    public int Total => Hp + Atk + Def + Spd + Luck;
}

/// <summary>O que o caçador leva para a missão: os 3 slots e até 5 cartas na mão, todos do inventário.</summary>
public sealed record Loadout(Equipment Equipment, IReadOnlyList<string> Hand)
{
    public const int MaxHand = 5;
    public static Loadout Empty => new(Equipment.None, Array.Empty<string>());

    public IEnumerable<string> EquippedIds()
    {
        if (Equipment.Weapon != null) yield return Equipment.Weapon;
        if (Equipment.Armor != null) yield return Equipment.Armor;
        if (Equipment.Accessory != null) yield return Equipment.Accessory;
    }
}

/// <summary>
/// O caçador persistente (GDD: jogador + caçador + inventário + rank). Guardado no dispositivo (offline)
/// ou no servidor (online). PV volta ao máximo entre missões, então não é guardado.
/// </summary>
public sealed record Profile(
    string Id,
    string Name,
    int ColorIndex,
    int FaceIndex,
    int Level,
    int Xp,
    StatPoints Points,
    int UnspentPoints,
    int Gold,
    int BankedGold,
    IReadOnlyList<string> Inventory,
    Loadout Loadout,
    int RankPoints,
    string Season,
    string? Skin)
{
    public static Profile New(string id, string name, int colorIndex = 0, int faceIndex = 0, string season = "") =>
        new(id, name, colorIndex, faceIndex, Level: 1, Xp: 0, StatPoints.None, UnspentPoints: 0, Gold: 0, BankedGold: 0,
            Array.Empty<string>(), Loadout.Empty, RankPoints: 0, season, Skin: null);

    public int TotalGold => Gold + BankedGold;
}
