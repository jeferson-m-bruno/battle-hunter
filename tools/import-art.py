#!/usr/bin/env python3
"""
Monta os sprites de client/Assets/_Project/Art/Resources/Art a partir de três pacotes CC0:

  - Isometric Stone Soup (Screaming Brain Studios)  -> chão e paredes isométricos 64x32 / 64x64
  - Dungeon Crawl Stone Soup 32x32 tiles (Full)     -> criaturas, baús, itens e ícones
  - Kenney Fantasy UI Borders                       -> molduras 9-slice de painéis, botões e cartas

Uso:
  python tools/import-art.py <dir-com-os-zips-extraídos>

O diretório precisa conter as pastas `iso/`, `dcss/` e `ui/` (zips já extraídos). Nunca edite os PNG de saída à mão:
rode este script de novo. O pós-processador do Unity (Scripts/Editor/ArtImportSettings.cs) aplica pivô, PPU e borda.
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "client" / "Assets" / "_Project" / "Art" / "Resources" / "Art"

MAGENTA = (255, 0, 255)

# Cores dos caçadores (Palette.Hunters): amarelo, azul, vermelho, verde.
HUNTER_COLORS = [(242, 191, 51), (77, 153, 242), (217, 77, 89), (128, 204, 102)]

CREDITS: list[tuple[str, str]] = []


def credit(key: str, source: str) -> None:
    CREDITS.append((key, source))


def demagenta(im: Image.Image) -> Image.Image:
    im = im.convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if r > 240 and g < 20 and b > 240:
                px[x, y] = (0, 0, 0, 0)
    return im


class Sources:
    def __init__(self, base: Path):
        self.iso = base / "iso" / "Tile-Sets"
        dcss_root = base / "dcss"
        inner = [p for p in dcss_root.iterdir() if p.is_dir()]
        self.dcss = inner[0] if len(inner) == 1 else dcss_root
        self.ui = base / "ui" / "PNG" / "Default"
        for p in (self.iso, self.dcss, self.ui):
            if not p.exists():
                sys.exit(f"pasta não encontrada: {p}")

    def floor(self, name: str, cell: int = 0) -> Image.Image:
        sheet = demagenta(Image.open(self.iso / "Floors" / f"ISS_Floor_{name}-64x32.png"))
        cols = sheet.width // 64
        x, y = (cell % cols) * 64, (cell // cols) * 32
        credit_name = f"Isometric Stone Soup: Floors/ISS_Floor_{name} célula {cell}"
        return sheet.crop((x, y, x + 64, y + 32)), credit_name

    def block(self, name: str, cell: int = 0) -> Image.Image:
        sheet = demagenta(Image.open(self.iso / "Walls" / "Block" / f"ISS_Block_{name}-64x64.png"))
        x = cell * 64
        return sheet.crop((x, 0, x + 64, 64)), f"Isometric Stone Soup: Walls/Block/ISS_Block_{name} célula {cell}"

    def dcss_tile(self, *candidates: str) -> tuple[Image.Image, str]:
        for rel in candidates:
            p = self.dcss / rel
            if p.exists():
                return Image.open(p).convert("RGBA"), f"DCSS: {rel}"
        sys.exit(f"nenhum dos candidatos existe no DCSS: {candidates}")

    def kenney(self, name: str) -> tuple[Image.Image, str]:
        p = self.ui / "Border" / f"{name}.png"
        return Image.open(p).convert("RGBA"), f"Kenney Fantasy UI Borders: Default/Border/{name}"


def save(key: str, im: Image.Image, source: str) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    im.save(OUT / f"{key}.png", optimize=True)
    credit(key, source)
    print(f"  {key:16} {im.width}x{im.height}  <- {source}")


def layer(base: Image.Image, *parts: Image.Image) -> Image.Image:
    out = base.copy()
    for part in parts:
        out.alpha_composite(part)
    return out


def tint(im: Image.Image, rgb: tuple[int, int, int]) -> Image.Image:
    out = im.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r * rgb[0] // 255, g * rgb[1] // 255, b * rgb[2] // 255, a)
    return out


def bright_only(im: Image.Image, threshold: int = 110) -> Image.Image:
    """Mantém só os pixels claros (para tirar o fundo de pedra escuro dos ícones de gateway)."""
    out = im.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a and (r + g + b) // 3 < threshold:
                px[x, y] = (0, 0, 0, 0)
    return out


def paste_center(canvas: Image.Image, im: Image.Image, dy: int = 0) -> Image.Image:
    out = canvas.copy()
    out.alpha_composite(im, ((out.width - im.width) // 2, (out.height - im.height) // 2 + dy))
    return out


def ground_shadow(color: tuple[int, int, int]) -> Image.Image:
    """Elipse colorida sob os pés: identifica o caçador mesmo com arte parecida."""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((6, 25, 25, 31), fill=color + (190,))
    return im


def frame_with_fill(border: Image.Image, fill_level: int) -> Image.Image:
    """Moldura Kenney (centro transparente) + preenchimento cinza; o tint do uGUI colore moldura e centro juntos."""
    out = border.copy()
    px = out.load()
    inner = inner_box(border)
    for y in range(inner[1], inner[3] + 1):
        for x in range(inner[0], inner[2] + 1):
            if px[x, y][3] == 0:
                px[x, y] = (fill_level, fill_level, fill_level, 255)
    return out


def inner_box(border: Image.Image) -> tuple[int, int, int, int]:
    """Maior região central transparente: define a borda 9-slice."""
    px = border.load()
    cx, cy = border.width // 2, border.height // 2
    left = cx
    while left > 0 and px[left - 1, cy][3] == 0:
        left -= 1
    right = cx
    while right < border.width - 1 and px[right + 1, cy][3] == 0:
        right += 1
    top = cy
    while top > 0 and px[cx, top - 1][3] == 0:
        top -= 1
    bottom = cy
    while bottom < border.height - 1 and px[cx, bottom + 1][3] == 0:
        bottom += 1
    return left, top, right, bottom


def build_tiles(src: Sources) -> None:
    print("tiles")
    floor, floor_src = src.floor("Limestone")
    save("tile_floor", floor, floor_src)

    spawn, spawn_src = src.floor("Cobble_Blood")
    save("tile_spawn", spawn, spawn_src)

    star, star_src = src.dcss_tile("dungeon/gateways/exit.png")
    star = bright_only(star).resize((22, 22), Image.NEAREST)
    exit_tile = paste_center(tint(floor, (170, 230, 185)), star)
    save("tile_exit", exit_tile, f"{floor_src} (tint verde) + {star_src} (só a estrela)")

    wall, wall_src = src.block("Stone_Brick")
    save("tile_wall", wall, wall_src)

    # Bloco de meia altura (célula ímpar da folha): usado nas paredes que têm chão atrás, para não esconder a célula.
    low, low_src = src.block("Stone_Brick", 1)
    save("tile_wall_low", low.crop((0, 16, 64, 64)), f"{low_src} (meia altura, 64×48)")


def build_objects(src: Sources) -> None:
    print("objetos")
    closed, closed_src = src.dcss_tile("dungeon/chest_2_closed.png", "dungeon/chest.png")
    save("chest", closed, closed_src)
    opened, opened_src = src.dcss_tile("dungeon/chest_2_open.png")
    save("chest_open", opened, opened_src)

    deck, deck_src = src.dcss_tile("item/misc/misc_deck_new.png", "item/misc/misc_deck_old.png")
    save("ground", deck, deck_src)

    halo, halo_src = src.dcss_tile("player/halo/halo_player.png")
    save("mark", halo, halo_src)

    # Mímico: baú fechado com olhos vermelhos e dentes na fresta da tampa.
    mimic = closed.copy()
    d = ImageDraw.Draw(mimic)
    d.rectangle((10, 13, 12, 15), fill=(230, 30, 30, 255))
    d.rectangle((19, 13, 21, 15), fill=(230, 30, 30, 255))
    for x in range(8, 24, 3):
        d.rectangle((x, 17, x + 1, 19), fill=(245, 245, 235, 255))
    save("monster_mimic", mimic, f"{closed_src} + olhos e dentes desenhados pelo script")


def build_hunters(src: Sources) -> None:
    print("caçadores")
    looks = [
        ("player/base/human_male.png", "player/body/leather_armor.png", "player/legs/pants_orange.png", "player/hair/short_yellow.png", "player/hand_right/axe.png"),
        ("player/base/human_female.png", "player/body/metal_blue.png", "player/legs/pants_blue.png", "player/hair/long_black.png", "player/hand_right/spear.png"),
        ("player/base/human_male.png", "player/body/leather_red.png", "player/legs/pants_red.png", "player/hair/short_red.png", "player/hand_right/long_sword.png"),
        ("player/base/elf_female.png", "player/body/leather_green.png", "player/legs/pants_darkgreen.png", "player/hair/elf_yellow.png", "player/hand_right/bow.png"),
    ]
    for i, parts in enumerate(looks):
        images = []
        sources = []
        for rel in parts:
            im, s = src.dcss_tile(rel, rel.replace(".png", "_new.png"), rel.replace(".png", "_old.png"))
            images.append(im)
            sources.append(rel)
        sprite = layer(ground_shadow(HUNTER_COLORS[i]), *images)
        save(f"hunter_{i}", sprite, "DCSS paper doll: " + " + ".join(sources) + " + sombra colorida do script")


def build_monsters(src: Sources) -> None:
    print("monstros")
    picks = {
        "kobold": ("monster/kobold_new.png", "monster/kobold.png"),
        "skeleton": ("monster/undead/skeletons/skeleton_humanoid_small_new.png", "monster/undead/skeletons/skeleton_small.png"),
        "spider": ("monster/animals/wolf_spider_new.png", "monster/animals/spider.png"),
        "orc": ("monster/orc_warrior_new.png", "monster/orc_new.png"),
        "dragon": ("monster/dragons/dragon.png", "monster/dragons/golden_dragon.png"),
    }
    for key, candidates in picks.items():
        im, s = src.dcss_tile(*candidates)
        save(f"monster_{key}", im, s)


def build_ui(src: Sources) -> None:
    print("ui")
    frames = {
        "panel": ("panel-border-004", 102),
        "button": ("panel-border-015", 190),
        "card_frame": ("panel-border-002", 178),
    }
    for key, (name, fill) in frames.items():
        border, s = src.kenney(name)
        framed = frame_with_fill(border, fill)
        framed = framed.resize((framed.width * 2, framed.height * 2), Image.NEAREST)
        save(key, framed, f"{s} (centro preenchido, 2x)")

    icons = {
        "icon_weapon": ("item/weapon/long_sword_1_new.png", "item/weapon/long_sword_2.png"),
        "icon_armor": ("item/armor/torso/chain_mail_1.png", "item/armor/torso/plate_1.png"),
        "icon_accessory": ("item/ring/diamond.png", "item/ring/agate.png"),
        "icon_consumable": ("item/potion/brilliant_blue_new.png", "item/potion/bubbly.png"),
        "icon_trap": ("dungeon/traps/trap_blade.png", "dungeon/traps/trap_mechanical.png"),
        "icon_special": ("gui/spells/fire/fireball_new.png", "gui/spells/fire/bolt_of_fire_new.png"),
        "icon_treasure": ("item/misc/misc_orb.png", "item/misc/misc_crystal_new.png"),
        "icon_gold": ("item/gold/gold_pile_10.png", "item/gold/gold_pile.png"),
        "icon_xp": ("item/misc/misc_rune.png", "item/misc/runes/rune_spider.png"),
    }
    for key, candidates in icons.items():
        im, s = src.dcss_tile(*candidates)
        save(key, im, s)


def write_credits() -> None:
    lines = [
        "# Créditos da arte",
        "",
        "Todos os pacotes são CC0 (domínio público). Atribuição não é exigida, mas fica aqui por respeito aos autores.",
        "Arquivos gerados por `tools/import-art.py`; não edite à mão.",
        "",
        "- **Isometric Stone Soup** — Screaming Brain Studios — https://opengameart.org/content/isometric-stone-soup",
        "- **Dungeon Crawl Stone Soup 32x32 tiles** — equipe e colaboradores do DCSS — https://opengameart.org/content/dungeon-crawl-32x32-tiles",
        "- **Fantasy UI Borders** — Kenney — https://kenney.nl / https://opengameart.org/content/fantasy-ui-borders",
        "",
        "| Chave | Origem |",
        "| --- | --- |",
    ]
    for key, source in CREDITS:
        lines.append(f"| `{key}` | {source} |")
    (OUT / "CREDITS.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"CREDITS.md com {len(CREDITS)} entradas")


def main() -> None:
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    src = Sources(Path(sys.argv[1]))
    build_tiles(src)
    build_objects(src)
    build_hunters(src)
    build_monsters(src)
    build_ui(src)
    write_credits()
    print(f"saída: {OUT}")


if __name__ == "__main__":
    main()
