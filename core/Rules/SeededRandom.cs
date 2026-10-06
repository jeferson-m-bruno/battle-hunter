using System;
namespace BattleHunter.Core.Rules;

/// <summary>
/// PRNG xorshift32 determinístico por seed. Implementação própria porque
/// System.Random muda de algoritmo entre runtimes (.NET 8 no servidor, Mono no Unity)
/// e a mesma seed precisa dar a mesma partida nos dois lados.
/// </summary>
public sealed class SeededRandom : IRandom
{
    private uint _state;

    public SeededRandom(int seed)
    {
        // xorshift nunca sai do zero; uma seed 0 vira uma constante não nula.
        _state = seed == 0 ? 0x9E3779B9u : (uint)seed;
    }

    public int NextD6() => Next(6) + 1;

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));

        return (int)(NextUInt() % (uint)maxExclusive);
    }

    private uint NextUInt()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }
}
