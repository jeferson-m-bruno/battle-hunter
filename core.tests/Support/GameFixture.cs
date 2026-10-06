using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Tests.Support;

/// <summary>
/// Atalhos para montar uma partida de teste e aplicar ações encadeadas.
/// </summary>
public static class GameFixture
{
    public static Hunter HunterAt(int id, int x, int y, HunterStats? stats = null) =>
        Hunter.Create(id, $"Caçador {id}", stats ?? HunterStats.Base, new Position(x, y));

    public static GameState NewGame(GridMap map, params Hunter[] hunters) =>
        GameState.New(new GameConfig(), map, hunters);

    public static GameState NewGame(GameConfig config, GridMap map, params Hunter[] hunters) =>
        GameState.New(config, map, hunters);

    /// <summary>Partida 5×5 aberta com 2 caçadores, já iniciada; retorna o estado e os eventos de abertura.</summary>
    public static (GameState State, IReadOnlyList<GameEvent> Events) Started(IRandom random, params Hunter[] hunters)
    {
        var hs = hunters.Length == 0 ? new[] { HunterAt(1, 0, 0), HunterAt(2, 4, 0) } : hunters;
        var state = NewGame(MapBuilder.Open5x5(), hs);
        var result = Reducer.Apply(state, new StartGame(), random);
        return (result.State, result.Events);
    }

    /// <summary>Partida iniciada e com o dado do caçador atual já rolado.</summary>
    public static GameState Rolled(IRandom random, params Hunter[] hunters)
    {
        var (state, _) = Started(random, hunters);
        return Reducer.Apply(state, new RollDice(state.CurrentHunterId), random).State;
    }

    public static ReducerResult Apply(this GameState state, GameAction action, IRandom random) =>
        Reducer.Apply(state, action, random);
}
