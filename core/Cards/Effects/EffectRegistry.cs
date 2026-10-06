using System.Collections.Generic;
using System.Linq;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>Registro id → efeito. Toda carta com "effect" no JSON precisa de uma entrada aqui.</summary>
public sealed class EffectRegistry
{
    private readonly Dictionary<string, ICardEffect> _effects;

    public EffectRegistry(IEnumerable<ICardEffect> effects)
    {
        _effects = effects.ToDictionary(e => e.Id, System.StringComparer.Ordinal);
    }

    /// <summary>Registro com todos os efeitos do jogo. Novo efeito = nova classe aqui + teste + entrada no JSON.</summary>
    public static EffectRegistry Default { get; } = new(new ICardEffect[]
    {
        new HealEffect(),
        new CurePoisonEffect(),
        new GainActionPointsEffect(),
        new BombEffect(),
        new ThrowEffect(),
        new TrapEffect(),
        new StrikeEffect(),
        new DoubleStrikeEffect(),
        new ChargeEffect(),
        new TripEffect(),
        new WhirlwindEffect(),
        new LifeStealEffect(),
    });

    public IReadOnlyCollection<string> Ids => _effects.Keys;

    public bool Contains(string id) => _effects.ContainsKey(id);

    public ICardEffect Get(string id) =>
        _effects.TryGetValue(id, out var effect)
            ? effect
            : throw new KeyNotFoundException($"Efeito desconhecido: '{id}'.");
}
