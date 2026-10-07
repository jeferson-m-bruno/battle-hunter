using BattleHunter.Core.State;
using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>Conversão grid ↔ mundo isométrico 2:1. Célula de 1 unidade de largura por 0,5 de altura.</summary>
    public static class Iso
    {
        public const float CellWidth = 1f;
        public const float CellHeight = 0.5f;

        public static Vector3 ToWorld(Position p, float z = 0f) => ToWorld(p.X, p.Y, z);

        public static Vector3 ToWorld(float x, float y, float z = 0f) =>
            new((x - y) * CellWidth * 0.5f, -(x + y) * CellHeight * 0.5f, z);

        public static Position ToCell(Vector3 world)
        {
            var a = world.x / (CellWidth * 0.5f);
            var b = -world.y / (CellHeight * 0.5f);
            return new Position(Mathf.RoundToInt((a + b) * 0.5f), Mathf.RoundToInt((b - a) * 0.5f));
        }

        /// <summary>Ordem de desenho: quem está mais "embaixo" (x+y maior) cobre quem está atrás.</summary>
        public static int SortingOrder(Position p, int layer) => (p.X + p.Y) * 4 + layer;

        public static Vector3 Center(int width, int height) => ToWorld((width - 1) * 0.5f, (height - 1) * 0.5f);
    }
}
