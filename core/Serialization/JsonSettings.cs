using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BattleHunter.Core.Serialization;

/// <summary>Convenção dos JSON de data/: chaves em camelCase, enums em snake_case ("special_attack").</summary>
public static class JsonSettings
{
    public static JsonSerializerSettings Default { get; } = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
        Converters = { new StringEnumConverter(new SnakeCaseNamingStrategy()) },
        MissingMemberHandling = MissingMemberHandling.Error,
    };
}
