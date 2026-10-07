using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BattleHunter.Core.Serialization;

/// <summary>JSON do protocolo cliente↔servidor: camelCase, enums como texto, ações/eventos/mensagens com discriminador "type".</summary>
public static class MessageJson
{
    public static JsonSerializerSettings Settings { get; } = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
        Converters =
        {
            new StringEnumConverter(),
            new PolymorphicConverter<GameAction>(),
            new PolymorphicConverter<GameEvent>(),
            new PolymorphicConverter<ClientMessage>(),
            new PolymorphicConverter<ServerMessage>(),
        },
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Formatting = Formatting.None,
    };

    public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, typeof(T), Settings);

    public static T Deserialize<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, Settings) ?? throw new JsonSerializationException($"JSON vazio para {typeof(T).Name}.");
}
