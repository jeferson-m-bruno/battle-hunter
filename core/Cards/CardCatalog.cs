using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards.Effects;
using BattleHunter.Core.Serialization;
using Newtonsoft.Json;

namespace BattleHunter.Core.Cards;

/// <summary>Todas as cartas do jogo, carregadas de data/cards.json.</summary>
public sealed class CardCatalog
{
    private static readonly IReadOnlyDictionary<string, int> NoParams = new Dictionary<string, int>();

    private readonly Dictionary<string, Card> _cards;

    public CardCatalog(IEnumerable<Card> cards)
    {
        _cards = cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<Card> All => _cards.Values;

    public Card Get(string id) =>
        _cards.TryGetValue(id, out var card)
            ? card
            : throw new KeyNotFoundException($"Carta desconhecida: '{id}'.");

    public bool Contains(string id) => _cards.ContainsKey(id);

    public IReadOnlyList<Card> Where(Func<Card, bool> predicate) => _cards.Values.Where(predicate).ToList();

    public static CardCatalog FromJson(string json)
    {
        var dtos = JsonConvert.DeserializeObject<List<CardDto>>(json, JsonSettings.Default)
                   ?? throw new JsonException("cards.json vazio.");

        return new CardCatalog(dtos.Select(d => d.ToCard()));
    }

    /// <summary>Erros de consistência: ids duplicados são impedidos no construtor; aqui validam-se efeitos e tipos.</summary>
    public IReadOnlyList<string> Validate(EffectRegistry effects)
    {
        var errors = new List<string>();
        foreach (var card in _cards.Values)
        {
            if (card.Effect != null && !effects.Contains(card.Effect))
                errors.Add($"Carta '{card.Id}' usa efeito não registrado '{card.Effect}'.");

            if (card.IsUsable && card.Effect == null)
                errors.Add($"Carta '{card.Id}' do tipo {card.Type} precisa de um efeito.");

            if (card.Type == CardType.Treasure && card.Mods != StatMods.None)
                errors.Add($"Tesouro '{card.Id}' não pode ter modificadores.");
        }

        return errors;
    }

    private sealed class CardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public CardType Type { get; set; }
        public Rarity Rarity { get; set; }
        public int Cost { get; set; }
        public int Sell { get; set; }
        public StatMods? Mods { get; set; }
        public string? Effect { get; set; }
        public Dictionary<string, int>? Params { get; set; }

        public Card ToCard() => new(Id, Name, Type, Rarity, Cost, Sell, Mods ?? StatMods.None, Effect, Params ?? NoParams);
    }
}
