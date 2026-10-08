using UnityEditor;
using UnityEngine;

namespace BattleHunter.Client.Editor
{
    /// <summary>
    /// Import reproduzível dos PNG de Assets/_Project/Art/Resources/Art (gerados por tools/import-art.py):
    /// sprite único, filtro Point, sem compressão, e pivô/PPU/borda 9-slice decididos pelo prefixo do nome,
    /// para que trocar o pacote de arte nunca exija mexer em .meta à mão.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Art/Resources/Art/";

        /// <summary>Mudou a regra de import? Suba a versão para o Unity reimportar os PNG já importados.</summary>
        public override uint GetVersion() => 2;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png"))
                return;

            var importer = (TextureImporter)assetImporter;
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 256;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;

            if (name == "tile_wall")
            {
                settings.spritePixelsPerUnit = 64;
                settings.spritePivot = new Vector2(0.5f, 0.25f); // centro do losango da base do bloco 64×64
            }
            else if (name == "tile_wall_low")
            {
                settings.spritePixelsPerUnit = 64;
                settings.spritePivot = new Vector2(0.5f, 1f / 3f); // idem, bloco de meia altura 64×48
            }
            else if (name.StartsWith("tile_"))
            {
                settings.spritePixelsPerUnit = 64;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
            }
            else if (name is "panel" or "button" or "card_frame")
            {
                // molduras Kenney de 48 px exportadas em 2x (96×96) com 16 px de borda original
                settings.spritePixelsPerUnit = 100;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteBorder = new Vector4(32, 32, 32, 32);
            }
            else if (name.StartsWith("icon_"))
            {
                settings.spritePixelsPerUnit = 100;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
            }
            else if (name is "mark" or "ground")
            {
                settings.spritePixelsPerUnit = 48;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                // criaturas e baús: ~2/3 da célula, em pé sobre o centro dela
                settings.spritePixelsPerUnit = 48;
                settings.spritePivot = new Vector2(0.5f, 0.1f);
            }

            importer.SetTextureSettings(settings);
        }
    }
}
