using BattleHunter.Core.Cards;
using UnityEngine;

namespace BattleHunter.Client
{
    /// <summary>Carrega data/*.json (sincronizados para Assets/_Project/Data/Resources) no GameContent do core.</summary>
    public static class ContentLoader
    {
        private static GameContent _content;

        public static GameContent Load()
        {
            if (_content != null)
                return _content;

            _content = GameContent.FromJson(
                Text("cards"),
                Text("monsters"),
                Text("loot_tables"),
                aiWeightsJson: Text("ai_weights"));
            return _content;
        }

        private static string Text(string name)
        {
            var asset = Resources.Load<TextAsset>(name);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/{name}.json não encontrado. Rode tools/sync-data.sh.");
            return asset.text;
        }
    }
}
