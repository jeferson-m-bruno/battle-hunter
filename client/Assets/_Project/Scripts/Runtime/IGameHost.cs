using System;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Client
{
    /// <summary>
    /// O que a apresentação precisa de uma partida, offline ou online: o snapshot do jogador (nunca o GameState cru),
    /// os eventos na ordem, e um canal para enviar intenções.
    /// </summary>
    public interface IGameHost
    {
        GameContent Content { get; }
        int HumanId { get; }
        PlayerSnapshot View { get; }
        bool IsHumanTurn { get; }
        float TurnTimeLeft { get; }
        string Title { get; }

        bool Submit(GameAction action);

        event Action<GameEvent> OnEvent;
        event Action OnStateChanged;
        event Action<MatchOutcome> OnFinished;
    }
}
