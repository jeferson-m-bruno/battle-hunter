using System.Collections.Generic;
using BattleHunter.Core.State;

namespace BattleHunter.Core.Ai;

/// <summary>O que um agente de IA lembra entre turnos: células já vistas. Fica fora do GameState, por agente.</summary>
public sealed class AiMemory
{
    private readonly HashSet<Position> _seen = new();

    public IReadOnlyCollection<Position> Seen => _seen;

    public bool HasSeen(Position p) => _seen.Contains(p);

    public void Observe(HunterView view)
    {
        foreach (var cell in view.VisibleCells)
            _seen.Add(cell);
    }
}
