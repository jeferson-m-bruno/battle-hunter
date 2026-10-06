using System;
using System.Collections.Generic;
using System.Linq;
using BattleHunter.Core.Cards;
using BattleHunter.Core.Map;
using BattleHunter.Core.Rules;
using BattleHunter.Core.State;
using BattleHunter.Core.State.Actions;
using BattleHunter.Core.State.Events;

namespace BattleHunter.Core.Ai;

/// <summary>Parâmetros de uma partida IA×IA.</summary>
public sealed record MatchSettings(
    MapSpec Map,
    GameConfig Config,
    int StartingCommonCards = 2,
    int MaxActions = 20_000)
{
    /// <summary>Caça (fácil): 12×12, 30 rodadas, sem chefe.</summary>
    public static MatchSettings Easy => new(new MapSpec(Size: 12), new GameConfig(MaxRounds: 30, BossRound: 0));

    /// <summary>Caça (normal): 14×14, 30 rodadas, chefe.</summary>
    public static MatchSettings Normal => new(new MapSpec(Size: 14), new GameConfig(MaxRounds: 30, BossRound: 15));

    /// <summary>Caça (difícil): 16×16, 25 rodadas, chefe com PV ×1.5.</summary>
    public static MatchSettings Hard => new(new MapSpec(Size: 16), new GameConfig(MaxRounds: 25, BossRound: 15, BossHpPercent: 150));
}

/// <summary>Resultado de uma partida IA×IA.</summary>
public sealed record MatchResult(
    int Seed,
    int Rounds,
    GameEndReason Reason,
    int? WinnerId,
    AiProfile? WinnerProfile,
    IReadOnlyDictionary<int, AiProfile> Profiles,
    int Fallen,
    int Exited,
    int Actions,
    int Rejections,
    bool Aborted);

/// <summary>
/// Roda uma partida inteira só com IA, sem renderização, sobre o mesmo Reducer que o servidor usa.
/// Determinística por seed. Serve ao simulador (tools/simulate.sh) e ao teste de fumaça.
/// </summary>
public static class AiMatch
{
    public static readonly AiProfile[] DefaultProfiles = { AiProfile.Aggressive, AiProfile.Cautious, AiProfile.Greedy, AiProfile.Balanced };

    public static MatchResult Play(int seed, MatchSettings settings, GameContent content, IReadOnlyList<AiProfile>? profiles = null)
    {
        var random = new SeededRandom(seed);
        var map = MapGenerator.Generate(settings.Map, random);
        var assigned = (profiles ?? DefaultProfiles).Take(MapSpec.HunterSpawns).ToList();

        var commons = content.Cards.Where(c => c.Rarity == Rarity.Common && c.Type != CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        var treasures = content.Cards.Where(c => c.Type == CardType.Treasure).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();

        var setups = assigned.Select((profile, i) =>
        {
            var hand = Enumerable.Range(0, settings.StartingCommonCards).Select(_ => commons[random.Next(commons.Count)].Id).ToList();
            return new HunterSetup(i + 1, $"{profile}", HunterStats.Base, hand);
        }).ToList();

        var treasure = treasures[random.Next(treasures.Count)].Id;
        var state = GameSetup.Create(settings.Config, map, setups, treasure, random, content);
        state = Reducer.Apply(state, new StartGame(), random, content).State;

        var players = setups.Select((s, i) => new AiPlayer(s.Id, assigned[i])).ToDictionary(p => p.HunterId);
        var actions = 0;
        var rejections = 0;

        while (state.Phase != GamePhase.Finished && actions < settings.MaxActions)
        {
            var player = players[state.CurrentHunterId];
            var action = player.Next(state, content);
            var result = Reducer.Apply(state, action, random, content);
            actions++;

            if (result.Events.Any(e => e is ActionRejected))
            {
                rejections++;
                var fallback = state.Phase == GamePhase.AwaitingRoll ? (GameAction)new RollDice(player.HunterId) : new Pass(player.HunterId);
                result = Reducer.Apply(state, fallback, random, content);
            }

            state = result.State;
        }

        var aborted = state.Phase != GamePhase.Finished;
        var profileById = players.ToDictionary(p => p.Key, p => p.Value.Profile);

        return new MatchResult(
            seed,
            state.Round,
            state.EndReason ?? GameEndReason.RoundLimit,
            state.WinnerId,
            state.WinnerId != null ? profileById[state.WinnerId.Value] : null,
            profileById,
            state.Hunters.Count(h => h.Status == HunterStatus.Fallen),
            state.Hunters.Count(h => h.Status == HunterStatus.Exited),
            actions,
            rejections,
            aborted);
    }
}
