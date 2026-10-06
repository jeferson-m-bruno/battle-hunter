namespace BattleHunter.Core.Rules;

/// <summary>
/// Única fonte de aleatoriedade do núcleo. O servidor usa uma seed por partida;
/// os testes usam seed fixa ou valores pré-definidos.
/// </summary>
public interface IRandom
{
    /// <summary>Rola um dado de 6 faces: 1 a 6.</summary>
    int NextD6();

    /// <summary>Inteiro em [0, maxExclusive).</summary>
    int Next(int maxExclusive);
}
