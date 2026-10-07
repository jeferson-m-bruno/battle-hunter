using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Progression;

/// <summary>Validação do loadout contra o inventário e conversão do perfil em caçador de partida.</summary>
public static class LoadoutRules
{
    /// <summary>Motivo da recusa, ou null. Cada carta do loadout precisa existir no inventário (com multiplicidade).</summary>
    public static string? Validate(Profile profile, Loadout loadout, CardCatalog cards)
    {
        if (loadout.Hand.Count > Loadout.MaxHand)
            return $"No máximo {Loadout.MaxHand} cartas na mão.";

        var pool = profile.Inventory.ToList();
        foreach (var id in loadout.Hand.Concat(loadout.EquippedIds()))
        {
            if (!pool.Remove(id))
                return $"A carta '{id}' não está no inventário.";
        }

        foreach (var (slot, id) in new[] { (EquipmentSlot.Weapon, loadout.Equipment.Weapon), (EquipmentSlot.Armor, loadout.Equipment.Armor), (EquipmentSlot.Accessory, loadout.Equipment.Accessory) })
        {
            if (id == null)
                continue;
            if (!cards.Contains(id))
                return $"Carta desconhecida: '{id}'.";
            var card = cards.Get(id);
            if (!card.IsEquipment || Equipment.SlotFor(card.Type) != slot)
                return $"'{card.Name}' não vai no slot {slot}.";
        }

        if (loadout.Hand.Any(id => cards.Get(id).Type == CardType.Treasure))
            return "Tesouros não vão para a missão.";

        return null;
    }

    public static HunterSetup ToHunterSetup(Profile profile, int hunterId) =>
        new(hunterId, profile.Name, LevelRules.StatsFor(profile), profile.Loadout.Hand, profile.Loadout.Equipment, profile.Level);
}
