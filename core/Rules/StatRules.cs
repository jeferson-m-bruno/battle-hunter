using BattleHunter.Core.Cards;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Rules;

/// <summary>Atributos efetivos = base do caçador + modificadores das 3 cartas equipadas.</summary>
public static class StatRules
{
    public static HunterStats Effective(Hunter hunter, GameContent content)
    {
        var mods = StatMods.None;
        foreach (var cardId in new[] { hunter.Equipment.Weapon, hunter.Equipment.Armor, hunter.Equipment.Accessory })
        {
            if (cardId != null)
                mods = mods.Add(content.Cards.Get(cardId).Mods);
        }

        return hunter.Stats.Apply(mods);
    }
}
