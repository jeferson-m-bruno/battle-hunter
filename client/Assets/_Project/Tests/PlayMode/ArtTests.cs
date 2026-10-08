using System.Collections;
using BattleHunter.Client.Presentation;
using BattleHunter.Client.UI;
using BattleHunter.Core.Cards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace BattleHunter.Client.Tests
{
    /// <summary>
    /// Arte final (fatia 7): o pacote importado por tools/import-art.py substitui os placeholders e o catálogo o encontra.
    /// </summary>
    public class ArtTests
    {
        private static readonly string[] BoardKeys =
        {
            "tile_floor", "tile_wall", "tile_wall_low", "tile_exit", "tile_spawn", "chest", "chest_open", "ground",
            "hunter_0", "hunter_1", "hunter_2", "hunter_3", "mark",
            "monster_kobold", "monster_skeleton", "monster_spider", "monster_orc", "monster_mimic", "monster_dragon",
        };

        private static readonly string[] UiKeys =
        {
            "panel", "button", "card_frame",
            "icon_weapon", "icon_armor", "icon_accessory", "icon_consumable", "icon_trap", "icon_special", "icon_treasure",
            "icon_gold", "icon_xp",
        };

        [UnityTest]
        public IEnumerator Given_ArtPack_When_LoadingEveryKey_Then_SpriteExists()
        {
            foreach (var key in BoardKeys)
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + key), $"Resources/Art/{key} não encontrado");
            foreach (var key in UiKeys)
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + key), $"Resources/Art/{key} não encontrado");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_ArtPack_When_CatalogResolvesBoardKeys_Then_UsesPackNotPlaceholder()
        {
            Assert.AreEqual("tile_floor", SpriteCatalog.Tile("floor", Palette.Floor).texture.name);
            Assert.AreEqual("tile_wall", SpriteCatalog.Tile("wall", Palette.Wall).texture.name);
            Assert.AreEqual("chest_open", SpriteCatalog.Chest(true).texture.name);
            Assert.AreEqual("hunter_2", SpriteCatalog.Hunter(2).texture.name);
            Assert.AreEqual("monster_dragon", SpriteCatalog.Monster("dragon").texture.name);
            Assert.AreEqual("ground", SpriteCatalog.Ground().texture.name);
            Assert.AreEqual("mark", SpriteCatalog.Mark().texture.name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_ImportedTiles_When_Inspected_Then_SizesAndPivotsMatchTheIsoGrid()
        {
            var floor = SpriteCatalog.Tile("floor", Palette.Floor);
            Assert.AreEqual(64, floor.rect.width);
            Assert.AreEqual(32, floor.rect.height);
            Assert.AreEqual(64f, floor.pixelsPerUnit);
            Assert.AreEqual(16f, floor.pivot.y, 0.01f, "chão: pivô no centro do losango");

            var wall = SpriteCatalog.Tile("wall", Palette.Wall);
            Assert.AreEqual(64, wall.rect.width);
            Assert.AreEqual(64, wall.rect.height);
            Assert.AreEqual(16f, wall.pivot.y, 0.01f, "parede: pivô no centro da base do bloco");

            var low = SpriteCatalog.Tile("wall_low", Palette.Wall);
            Assert.AreEqual(64, low.rect.width);
            Assert.AreEqual(48, low.rect.height);
            Assert.AreEqual(16f, low.pivot.y, 0.01f, "parede baixa: mesma base, meia altura");

            var orc = SpriteCatalog.Monster("orc");
            Assert.Less(orc.pivot.y, orc.rect.height * 0.2f, "criaturas: pivô perto dos pés");
            Assert.AreEqual(48f, orc.pixelsPerUnit, "criaturas ocupam ~2/3 da célula");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_UiFrames_When_Loaded_Then_AreNineSliced()
        {
            foreach (var sprite in new[] { SpriteCatalog.Panel(), SpriteCatalog.Button(), SpriteCatalog.CardFrame() })
            {
                Assert.IsNotNull(sprite);
                Assert.Greater(sprite.border.x, 0f, sprite.name + ": borda 9-slice");
                Assert.Greater(sprite.border.y, 0f, sprite.name + ": borda 9-slice");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_CardTypes_When_AskingIcon_Then_EveryTypeHasOne()
        {
            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
                Assert.IsNotNull(SpriteCatalog.CardIcon(type), type + " sem ícone");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Given_UiFactory_When_BuildingPanelAndButton_Then_UsesFramesAndIcon()
        {
            var canvas = Ui.Canvas("ArtTestCanvas");
            var panel = Ui.PanelRect(canvas.transform, "P", Vector2.zero, Vector2.one);
            var image = panel.GetComponent<Image>();
            Assert.AreEqual(SpriteCatalog.Panel(), image.sprite);
            Assert.AreEqual(Image.Type.Sliced, image.type);

            var bg = Ui.PanelRect(canvas.transform, "Bg", Vector2.zero, Vector2.one, Ui.Background, framed: false);
            Assert.IsNull(bg.GetComponent<Image>().sprite, "fundo de tela não leva moldura");

            var button = Ui.Button(canvas.transform, "B", "Ok", Vector2.zero, Vector2.one, () => { }, icon: SpriteCatalog.Icon("icon_gold"));
            Assert.AreEqual(SpriteCatalog.Button(), button.GetComponent<Image>().sprite);
            var icon = button.transform.Find("Icon");
            Assert.IsNotNull(icon, "botão com ícone cria filho Icon");
            Assert.AreEqual("icon_gold", icon.GetComponent<Image>().sprite.texture.name);

            Object.Destroy(canvas.gameObject);
            yield return null;
        }
    }
}
