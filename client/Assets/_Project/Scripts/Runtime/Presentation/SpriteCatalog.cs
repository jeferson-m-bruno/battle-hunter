using System.Collections.Generic;
using BattleHunter.Core.Cards;
using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>
    /// Ponto único de troca de arte: procura um sprite em Resources/Art/{chave} (pacote importado por tools/import-art.py)
    /// e, se não há, usa o placeholder gerado em código (tabuleiro) ou nenhum sprite (UI fica na cor chapada).
    /// Chaves do tabuleiro: tile_floor, tile_wall, tile_exit, tile_spawn, chest, chest_open, ground, hunter_0..3, mark,
    /// monster_{tipo}. Chaves de UI: panel, button, card_frame, icon_{weapon,armor,accessory,consumable,trap,special,treasure,gold,xp}.
    /// </summary>
    public static class SpriteCatalog
    {
        private static readonly Dictionary<string, Sprite> Cache = new();
        private static readonly HashSet<string> Missing = new();

        public static Sprite Get(string key, System.Func<Sprite> fallback)
        {
            var loaded = Load(key);
            if (loaded != null)
                return loaded;

            if (Cache.TryGetValue("fallback:" + key, out var cached) && cached != null)
                return cached;
            return Cache["fallback:" + key] = fallback();
        }

        /// <summary>Sprite do pacote ou null (sem placeholder). Para UI: Image sem sprite é a cor chapada de antes.</summary>
        public static Sprite Load(string key)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            if (Missing.Contains(key))
                return null;

            var loaded = Resources.Load<Sprite>("Art/" + key);
            if (loaded == null)
            {
                Missing.Add(key);
                return null;
            }

            Cache[key] = loaded;
            return loaded;
        }

        // ---- tabuleiro ------------------------------------------------------------

        public static Sprite Tile(string key, Color color) => Get("tile_" + key, () => ProceduralSprites.Diamond(color, key));
        public static Sprite Chest(bool opened) => Get(opened ? "chest_open" : "chest", () => ProceduralSprites.Shape(opened ? Palette.ChestOpened : Palette.ChestClosed, false, opened ? "chest_open" : "chest"));
        public static Sprite Hunter(int index) => Get("hunter_" + index, () => ProceduralSprites.Disc(Palette.Hunters[index % Palette.Hunters.Length], "h" + index));
        public static Sprite Monster(string typeId) => Get("monster_" + typeId, () => ProceduralSprites.Shape(Palette.Monster(typeId), true, typeId));
        public static Sprite Ground() => Get("ground", () => ProceduralSprites.Disc(Palette.Ground, "ground"));
        public static Sprite Mark() => Get("mark", () => ProceduralSprites.Disc(Palette.Marked, "mark"));

        // ---- UI -------------------------------------------------------------------

        public static Sprite Panel() => Load("panel");
        public static Sprite Button() => Load("button");
        public static Sprite CardFrame() => Load("card_frame");
        public static Sprite Icon(string key) => Load(key);

        public static Sprite CardIcon(CardType type) => Icon(type switch
        {
            CardType.Weapon => "icon_weapon",
            CardType.Armor => "icon_armor",
            CardType.Accessory => "icon_accessory",
            CardType.Consumable => "icon_consumable",
            CardType.Trap => "icon_trap",
            CardType.SpecialAttack => "icon_special",
            _ => "icon_treasure",
        });
    }
}
