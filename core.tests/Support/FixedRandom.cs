using BattleHunter.Core.Rules;

namespace BattleHunter.Core.Tests.Support;

/// <summary>
/// IRandom que devolve valores pré-definidos, para forçar um resultado de dado num teste.
/// Next(max) devolve sempre max-1: no Fisher-Yates isso troca o elemento consigo mesmo,
/// então a ordem de turno fica igual à ordem de criação dos caçadores.
/// </summary>
public sealed class FixedRandom : IRandom
{
    private readonly Queue<int> _dice;

    public FixedRandom(params int[] dice) => _dice = new Queue<int>(dice);

    public int NextD6() => _dice.Count > 0 ? _dice.Dequeue() : 1;

    public int Next(int maxExclusive) => maxExclusive - 1;
}
