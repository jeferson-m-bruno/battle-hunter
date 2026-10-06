namespace BattleHunter.Core.Map;

/// <summary>Parâmetros de geração (GDD, seção Mapa): grid de 12 a 16, 5 a 8 salas, 8 a 14 baús.</summary>
public sealed record MapSpec(
    int Size = 12,
    int MinRooms = 5,
    int MaxRooms = 8,
    int MinChests = 8,
    int MaxChests = 14)
{
    public const int HunterSpawns = 4;
}
