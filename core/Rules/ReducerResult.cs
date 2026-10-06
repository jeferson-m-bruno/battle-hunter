using System.Collections.Generic;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Rules;

public sealed record ReducerResult(GameState State, IReadOnlyList<GameEvent> Events);
