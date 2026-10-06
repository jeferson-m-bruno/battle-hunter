using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleHunter.Core.Cards.Effects;

/// <summary>Registro id → efeito. Toda carta com "effect" no JSON precisa de uma entrada aqui.</summary>
public sealed class EffectRegistry
{
    private readonly Dictionary<string, ICardEffect> _effects;

    public EffectRegistry(IEnumerable<ICardEffect> effects)
    {
        _effects = effects.ToDictionary(e => e.Id, StringComparer.Ordinal);
    }

    /// <summary>Registro com todos os efeitos do jogo. Vazio até a fatia 3.</summary>
    public static EffectRegistry Default { get; } = new(Array.Empty<ICardEffect>());

    public bool Contains(string id) => _effects.ContainsKey(id);

    public ICardEffect Get(string id) =>
        _effects.TryGetValue(id, out var effect)
            ? effect
            : throw new KeyNotFoundException($"Efeito desconhecido: '{id}'.");
}
