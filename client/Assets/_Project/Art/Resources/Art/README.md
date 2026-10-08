# Arte

Os PNG desta pasta são gerados por `tools/import-art.py` a partir de três pacotes CC0 (ver `CREDITS.md`):
Isometric Stone Soup (chão e paredes), Dungeon Crawl Stone Soup 32×32 (criaturas, itens, ícones) e
Kenney Fantasy UI Borders (molduras). **Não edite os PNG à mão**: ajuste o script e rode de novo.

Para trocar por arte própria, basta substituir os PNG mantendo os nomes abaixo. O `SpriteCatalog` procura por nome;
o que não existir continua com o placeholder gerado em código (tabuleiro) ou na cor chapada (UI).
`Scripts/Editor/ArtImportSettings.cs` aplica pivô, PPU, filtro Point e borda 9-slice pelo prefixo do nome.

## Tabuleiro

| Chave | O que é | Import |
| --- | --- | --- |
| `tile_floor`, `tile_exit`, `tile_spawn` | losango isométrico 2:1, 64×32 px por célula | PPU 64, pivô no centro |
| `tile_wall` | bloco isométrico 64×64 cuja base é o losango da célula | PPU 64, pivô (0.5, 0.25) |
| `tile_wall_low` | mesmo bloco em meia altura (64×48); o `BoardView` usa nas paredes que têm chão atrás, para não esconder a célula | PPU 64, pivô (0.5, 1/3) |
| `chest`, `chest_open` | baú fechado / aberto, 32×32 | PPU 48, pivô na base |
| `hunter_0` … `hunter_3` | caçadores pelas 4 cores (amarelo, azul, vermelho, verde), 32×32 | PPU 48, pivô na base |
| `monster_kobold`, `monster_skeleton`, `monster_spider`, `monster_orc`, `monster_mimic`, `monster_dragon` | monstros e chefe, 32×32 | PPU 48, pivô na base |
| `ground` | pilha de cartas no chão | PPU 48, pivô no centro |
| `mark` | marcador do portador do tesouro (anel sob o caçador) | PPU 48, pivô no centro |

## UI

| Chave | O que é | Import |
| --- | --- | --- |
| `panel`, `button`, `card_frame` | molduras 9-slice (96×96, borda 32) tingidas pela cor do painel/botão | PPU 100, borda 32 |
| `icon_weapon`, `icon_armor`, `icon_accessory`, `icon_consumable`, `icon_trap`, `icon_special`, `icon_treasure` | ícone por tipo de carta | PPU 100 |
| `icon_gold`, `icon_xp` | ouro e experiência | PPU 100 |
