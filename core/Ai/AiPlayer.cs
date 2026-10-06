using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;

namespace BattleHunter.Core.Ai;

/// <summary>Um caçador controlado pela IA: perfil fixo e memória própria. Usa a mesma API de ações de um humano.</summary>
public sealed class AiPlayer
{
    public AiPlayer(int hunterId, AiProfile profile)
    {
        HunterId = hunterId;
        Profile = profile;
    }

    public int HunterId { get; }
    public AiProfile Profile { get; }
    public AiMemory Memory { get; } = new();

    public GameAction Next(GameState state, GameContent content) =>
        HunterBrain.Decide(state, HunterId, Profile, Memory, content);
}
