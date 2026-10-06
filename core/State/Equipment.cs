using BattleHunter.Core.Cards;

namespace BattleHunter.Core.State;

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory,
}

/// <summary>Os 3 slots de equipamento, por id de carta. Visíveis a todos (GDD).</summary>
public sealed record Equipment(string? Weapon = null, string? Armor = null, string? Accessory = null)
{
    public static Equipment None => new();

    public string? this[EquipmentSlot slot] => slot switch
    {
        EquipmentSlot.Weapon => Weapon,
        EquipmentSlot.Armor => Armor,
        _ => Accessory,
    };

    public Equipment With(EquipmentSlot slot, string? cardId) => slot switch
    {
        EquipmentSlot.Weapon => this with { Weapon = cardId },
        EquipmentSlot.Armor => this with { Armor = cardId },
        _ => this with { Accessory = cardId },
    };

    public static EquipmentSlot SlotFor(CardType type) => type switch
    {
        CardType.Weapon => EquipmentSlot.Weapon,
        CardType.Armor => EquipmentSlot.Armor,
        CardType.Accessory => EquipmentSlot.Accessory,
        _ => throw new System.ArgumentException($"Carta do tipo {type} não equipa.", nameof(type)),
    };
}
