using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>Tabela de níveis 1 a 30 (data/levels.json): XP acumulado necessário para cada nível.</summary>
public sealed class LevelTable
{
    public const int MaxLevel = 30;

    private readonly int[] _xp;

    public LevelTable(IEnumerable<(int Level, int Xp)> rows)
    {
        var ordered = rows.OrderBy(r => r.Level).ToList();
        if (ordered.Count == 0 || ordered[0].Level != 1 || ordered[0].Xp != 0)
            throw new ArgumentException("A tabela de níveis precisa começar no nível 1 com XP 0.");

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Level != i + 1 || ordered[i].Xp <= ordered[i - 1].Xp)
                throw new ArgumentException($"Tabela de níveis inválida no nível {ordered[i].Level}.");
        }

        _xp = ordered.Select(r => r.Xp).ToArray();
    }

    public int Count => _xp.Length;

    /// <summary>XP acumulado para alcançar o nível.</summary>
    public int XpFor(int level) => _xp[Math.Clamp(level, 1, _xp.Length) - 1];

    /// <summary>Nível correspondente a um XP acumulado.</summary>
    public int LevelFor(int xp)
    {
        var level = 1;
        for (var i = 1; i < _xp.Length; i++)
        {
            if (xp >= _xp[i])
                level = i + 1;
            else
                break;
        }

        return level;
    }

    public static LevelTable FromJson(string json)
    {
        var rows = JsonConvert.DeserializeObject<List<Row>>(json, JsonSettings.Default) ?? throw new JsonException("levels.json vazio.");
        return new LevelTable(rows.Select(r => (r.Level, r.Xp)));
    }

    /// <summary>Curva padrão (25·(n−1)² + 75·(n−1)), igual a data/levels.json.</summary>
    public static LevelTable Default { get; } = new(Enumerable.Range(1, MaxLevel).Select(n => (n, 25 * (n - 1) * (n - 1) + 75 * (n - 1))));

    private sealed class Row
    {
        public int Level { get; set; }
        public int Xp { get; set; }
    }
}
