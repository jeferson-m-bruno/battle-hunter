using System.Collections.Generic;
using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>
    /// Ponto único de troca de arte: procura um sprite em Resources/Art/{chave} (pacote importado) e, se não há,
    /// usa o placeholder gerado em código. Chaves: tile_floor, tile_wall, tile_exit, tile_spawn, chest, chest_open,
    /// ground, hunter_0..3, mark, monster_{tipo} (kobold, skeleton, spider, orc, mimic, dragon).
    /// </summary>
    public static class SpriteCatalog
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Get(string key, System.Func<Sprite> fallback)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var loaded = Resources.Load<Sprite>("Art/" + key);
            var sprite = loaded != null ? loaded : fallback();
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Tile(string key, Color color) => Get("tile_" + key, () => ProceduralSprites.Diamond(color, key));
        public static Sprite Chest(bool opened) => Get(opened ? "chest_open" : "chest", () => ProceduralSprites.Shape(opened ? Palette.ChestOpened : Palette.ChestClosed, false, opened ? "chest_open" : "chest"));
        public static Sprite Hunter(int index) => Get("hunter_" + index, () => ProceduralSprites.Disc(Palette.Hunters[index % Palette.Hunters.Length], "h" + index));
        public static Sprite Monster(string typeId) => Get("monster_" + typeId, () => ProceduralSprites.Shape(Palette.Monster(typeId), true, typeId));
        public static Sprite Ground() => Get("ground", () => ProceduralSprites.Disc(Palette.Ground, "ground"));
        public static Sprite Mark() => Get("mark", () => ProceduralSprites.Disc(Palette.Marked, "mark"));
    }
}
