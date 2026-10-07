using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>Cores dos placeholders.</summary>
    public static class Palette
    {
        public static readonly Color Floor = new(0.55f, 0.50f, 0.42f);
        public static readonly Color Wall = new(0.22f, 0.20f, 0.24f);
        public static readonly Color Exit = new(0.25f, 0.75f, 0.45f);
        public static readonly Color ChestClosed = new(0.80f, 0.55f, 0.20f);
        public static readonly Color ChestOpened = new(0.45f, 0.35f, 0.20f);
        public static readonly Color Spawn = new(0.50f, 0.42f, 0.45f);
        public static readonly Color Fog = new(0f, 0f, 0f, 0.65f);
        public static readonly Color Reachable = new(0.35f, 0.65f, 1f, 0.45f);
        public static readonly Color Target = new(1f, 0.35f, 0.3f, 0.55f);
        public static readonly Color Ground = new(0.9f, 0.9f, 0.6f);
        public static readonly Color Marked = new(1f, 0.85f, 0.1f);

        public static readonly Color[] Hunters =
        {
            new(0.95f, 0.75f, 0.2f), new(0.3f, 0.6f, 0.95f), new(0.85f, 0.3f, 0.35f), new(0.5f, 0.8f, 0.4f),
        };

        public static Color Monster(string typeId) => typeId switch
        {
            "kobold" => new Color(0.6f, 0.8f, 0.3f),
            "skeleton" => new Color(0.9f, 0.9f, 0.85f),
            "spider" => new Color(0.4f, 0.2f, 0.5f),
            "orc" => new Color(0.3f, 0.5f, 0.25f),
            "mimic" => new Color(0.8f, 0.55f, 0.2f),
            "dragon" => new Color(0.9f, 0.15f, 0.1f),
            _ => Color.magenta,
        };
    }
}
