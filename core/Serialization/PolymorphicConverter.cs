using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BattleHunter.Core.Serialization;

/// <summary>
/// Serializa hierarquias de records com um discriminador "type" = nome do tipo concreto.
/// A lista de tipos é fechada (subclasses concretas de TBase neste assembly): nada de TypeNameHandling.
/// </summary>
public sealed class PolymorphicConverter<TBase> : JsonConverter
{
    private static readonly Dictionary<string, Type> ByName = typeof(TBase).Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && typeof(TBase).IsAssignableFrom(t))
        .ToDictionary(t => t.Name, StringComparer.Ordinal);

    public static IReadOnlyCollection<string> KnownTypes => ByName.Keys;

    public override bool CanConvert(Type objectType) => typeof(TBase).IsAssignableFrom(objectType);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var jo = JObject.FromObject(value, Inner(serializer));
        jo.AddFirst(new JProperty("type", value.GetType().Name));
        jo.WriteTo(writer);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var jo = JObject.Load(reader);
        var name = jo.Value<string>("type") ?? throw new JsonSerializationException($"Falta o campo 'type' para {typeof(TBase).Name}.");
        if (!ByName.TryGetValue(name, out var type))
            throw new JsonSerializationException($"Tipo desconhecido para {typeof(TBase).Name}: '{name}'.");

        if (!objectType.IsAssignableFrom(type))
            throw new JsonSerializationException($"'{name}' não é um {objectType.Name}.");

        jo.Remove("type");
        return jo.ToObject(type, Inner(serializer));
    }

    /// <summary>O mesmo serializador sem este conversor: o tipo concreto é escrito/lido como um record comum.</summary>
    private JsonSerializer Inner(JsonSerializer outer)
    {
        var inner = new JsonSerializer
        {
            ContractResolver = outer.ContractResolver,
            NullValueHandling = outer.NullValueHandling,
            MissingMemberHandling = outer.MissingMemberHandling,
            DefaultValueHandling = outer.DefaultValueHandling,
            Formatting = outer.Formatting,
        };
        foreach (var converter in outer.Converters)
        {
            if (!ReferenceEquals(converter, this))
                inner.Converters.Add(converter);
        }

        return inner;
    }
}
