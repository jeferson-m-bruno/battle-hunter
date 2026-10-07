using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Progression;

/// <summary>O que uma partida rendeu ao jogador (para a tela de resultado e o histórico).</summary>
public sealed record RewardSummary(
    bool Won,
    int XpGained,
    int LevelsGained,
    int GoldFound,
    int GoldFromMission,
    int GoldFromSales,
    int GoldLost,
    IReadOnlyList<string> CardsKept,
    IReadOnlyList<string> CardsSold,
    string? RareCard,
    int RankDelta);

/// <summary>
/// Fim de missão (GDD): até 5 cartas voltam ao inventário e o resto vira ouro; XP de monstros e baús mais o da
/// missão se venceu; ouro da missão; carta rara na difícil; ao cair perde-se 10% do ouro não depositado; rank na ranqueada.
/// </summary>
public static class MatchRewards
{
    public const int KeepCards = 5;
    public const int FallLossPercent = 10;

    public static (Profile Profile, RewardSummary Summary) Apply(
        Profile profile,
        Hunter final,
        bool won,
        MissionType mission,
        GameContent content,
        IRandom random,
        string season)
    {
        profile = RankRules.EnsureSeason(profile, season);

        // Inventário: tira o que foi levado, devolve o que voltou (mão até 5 + equipamento final).
        var inventory = profile.Inventory.ToList();
        foreach (var id in profile.Loadout.Hand.Concat(profile.Loadout.EquippedIds()))
            inventory.Remove(id);

        var kept = final.Hand.Take(KeepCards).ToList();
        var sold = final.Hand.Skip(KeepCards).ToList();
        inventory.AddRange(kept);
        inventory.AddRange(new Loadout(final.Equipment, Array.Empty<string>()).EquippedIds());

        string? rare = null;
        if (won && mission.RewardRareCard)
        {
            var rares = content.Cards.Where(c => c.Rarity == Rarity.Rare && c.Type != CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
            rare = rares[random.Next(rares.Count)].Id;
            inventory.Add(rare);
        }

        var salesGold = sold.Sum(id => content.Cards.Get(id).Sell);
        var missionGold = won ? mission.RewardGold : 0;
        var goldBefore = profile.Gold + final.Gold + salesGold + missionGold;
        var lost = final.Status == HunterStatus.Fallen ? goldBefore * FallLossPercent / 100 : 0;

        var xp = final.Xp + (won ? mission.RewardXp : 0);
        var (leveled, levels) = LevelRules.GrantXp(profile, xp, content.Levels);

        var rankDelta = mission.Ranked ? RankRules.Delta(won, final.Status) : 0;
        var rank = Math.Max(0, leveled.RankPoints + rankDelta);

        var updated = leveled with
        {
            Gold = goldBefore - lost,
            Inventory = inventory,
            Loadout = new Loadout(final.Equipment, kept.Take(Loadout.MaxHand).ToList()),
            RankPoints = rank,
        };

        return (updated, new RewardSummary(won, xp, levels, final.Gold, missionGold, salesGold, lost, kept, sold, rare, rank - leveled.RankPoints));
    }
}
