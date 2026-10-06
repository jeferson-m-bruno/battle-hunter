using System.Collections.Generic;
using BattleHunter.Core.Cards;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

/// <summary>Chefe (GDD): aparece na rodada configurada ou quando o tesouro-alvo é pego. O sopro entra no commit seguinte.</summary>
internal static class BossRules
{
    public static GameState SpawnIfDue(GameState state, IRandom random, GameContent content, List<GameEvent> events) => state;
}
