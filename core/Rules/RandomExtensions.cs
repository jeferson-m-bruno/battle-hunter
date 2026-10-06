using System.Collections.Generic;

namespace BattleHunter.Core.Rules;

public static class RandomExtensions
{
    /// <summary>Embaralha por Fisher-Yates e devolve uma nova lista; a original não muda.</summary>
    public static IReadOnlyList<T> Shuffle<T>(this IRandom random, IReadOnlyList<T> items)
    {
        var result = new List<T>(items);
        for (var i = result.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}
