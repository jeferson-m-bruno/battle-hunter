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
    public static Hunter HunterAt(int id, int x, int y, HunterStats? stats = null, IReadOnlyList<string>? hand = null, Equipment? equipment = null, int level = 1) =>
        Hunter.Create(id, $"Caçador {id}", stats ?? HunterStats.Base, new Position(x, y), hand, equipment, level);

    public static Monster MonsterAt(int id, string typeId, int x, int y, int? hp = null) =>
        new(id, typeId, hp ?? TestContent.Default.Monsters.Get(typeId).Hp, new Position(x, y));

    public static GameState NewGame(GridMap map, params Hunter[] hunters) =>
        GameState.New(new GameConfig(), map, hunters);

    public static GameState NewGame(GameConfig config, GridMap map, params Hunter[] hunters) =>
        GameState.New(config, map, hunters);

    public static GameState NewGameWithChests(GridMap map, IReadOnlyList<Chest> chests, string? treasureId, params Hunter[] hunters) =>
        GameState.New(new GameConfig(), map, hunters, chests, treasureId);

    /// <summary>Coloca monstros numa partida já montada (antes ou depois de iniciar).</summary>
    public static GameState WithMonsters(this GameState state, params Monster[] monsters) =>
        state with { Monsters = monsters.ToList(), NextMonsterId = monsters.Length == 0 ? 1 : monsters.Max(m => m.Id) + 1 };

    /// <summary>Partida 5×5 aberta com 2 caçadores, já iniciada; retorna o estado e os eventos de abertura.</summary>
    public static (GameState State, IReadOnlyList<GameEvent> Events) Started(IRandom random, params Hunter[] hunters)
    {
        var hs = hunters.Length == 0 ? new[] { HunterAt(1, 0, 0), HunterAt(2, 4, 0) } : hunters;
        var state = NewGame(MapBuilder.Open5x5(), hs);
        var result = state.Apply(new StartGame(), random);
        return (result.State, result.Events);
    }

    /// <summary>Partida iniciada e com o dado do caçador atual já rolado.</summary>
    public static GameState Rolled(IRandom random, params Hunter[] hunters)
    {
        var (state, _) = Started(random, hunters);
        return state.Apply(new RollDice(state.CurrentHunterId), random).State;
    }

    /// <summary>Inicia e rola o dado numa partida já montada (com baús, por exemplo).</summary>
    public static GameState StartAndRoll(this GameState state, IRandom random)
    {
        state = state.Apply(new StartGame(), random).State;
        return state.Apply(new RollDice(state.CurrentHunterId), random).State;
    }

    public static ReducerResult Apply(this GameState state, GameAction action, IRandom random) =>
        Reducer.Apply(state, action, random, TestContent.Default);
}
