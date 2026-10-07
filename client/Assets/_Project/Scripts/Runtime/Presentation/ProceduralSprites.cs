using System.Collections.Generic;
using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>Placeholders geométricos gerados em código (GDD: arte só a partir da fatia 7).</summary>
    public static class ProceduralSprites
    {
        private const int Ppu = 64;
        private static readonly Dictionary<string, Sprite> Cache = new();

        /// <summary>Losango 64×32 (uma célula isométrica).</summary>
        public static Sprite Diamond(Color color, string key) => Cached("diamond:" + key, () =>
        {
            const int w = 64, h = 32;
            var tex = NewTexture(w, h);
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f);
                    var dy = Mathf.Abs(y + 0.5f - h * 0.5f) / (h * 0.5f);
                    var inside = dx + dy <= 1f;
                    var edge = dx + dy > 0.9f;
                    tex.SetPixel(x, y, inside ? (edge ? color * 0.6f + Color.black * 0.4f : color) : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu);
        });

        /// <summary>Disco 40×40 com contorno escuro (caçadores).</summary>
        public static Sprite Disc(Color color, string key) => Cached("disc:" + key, () =>
        {
            const int s = 40;
            var tex = NewTexture(s, s);
            for (var y = 0; y < s; y++)
                for (var x = 0; x < s; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.5f, s * 0.5f)) / (s * 0.5f);
                    tex.SetPixel(x, y, d <= 1f ? (d > 0.85f ? Color.black : color) : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.15f), Ppu);
        });

        /// <summary>Triângulo (monstros) ou quadrado (baú), 40×40.</summary>
        public static Sprite Shape(Color color, bool triangle, string key) => Cached("shape:" + key, () =>
        {
            const int s = 40;
            var tex = NewTexture(s, s);
            for (var y = 0; y < s; y++)
                for (var x = 0; x < s; x++)
                {
                    var inside = triangle
                        ? Mathf.Abs(x + 0.5f - s * 0.5f) <= (y + 0.5f) * 0.5f && y < s - 2
                        : x > 2 && x < s - 3 && y > 2 && y < s - 3;
                    var edge = triangle
                        ? Mathf.Abs(x + 0.5f - s * 0.5f) > (y + 0.5f) * 0.5f - 3f || y >= s - 5
                        : x < 6 || x > s - 7 || y < 6 || y > s - 7;
                    tex.SetPixel(x, y, inside ? (edge ? Color.black : color) : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.15f), Ppu);
        });

        /// <summary>Quadrado branco 4×4 para barras e fundos.</summary>
        public static Sprite White() => Cached("white", () =>
        {
            var tex = NewTexture(4, 4);
            for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        });

        private static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            return tex;
        }

        private static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var sprite) || sprite == null)
                Cache[key] = sprite = make();
            return sprite;
        }
    }
}
