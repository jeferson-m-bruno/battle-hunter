#!/usr/bin/env python3
"""Gera docs/cards.md a partir de data/cards.json. Uso: python tools/gen-cards-doc.py"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TYPES = {
    "weapon": "Armas", "armor": "Armaduras", "accessory": "Acessórios",
    "consumable": "Consumíveis", "trap": "Armadilhas", "special_attack": "Ataques especiais",
    "treasure": "Tesouros",
}
RARITY = {"common": "comum", "uncommon": "incomum", "rare": "rara"}
MODS = {"hp": "PV", "atk": "ATQ", "def": "DEF", "spd": "VEL", "luck": "SOR"}


def effect_text(c):
    if not c.get("effect"):
        return "—"
    params = c.get("params") or {}
    inner = ", ".join(f"{k}={v}" for k, v in params.items())
    return f"`{c['effect']}`" + (f" ({inner})" if inner else "")


def mods_text(mods):
    if not mods:
        return "—"
    return ", ".join(f"{v:+d} {MODS[k]}" for k, v in mods.items() if v)


cards = json.loads((ROOT / "data" / "cards.json").read_text(encoding="utf-8"))
out = ["# Catálogo de cartas", "",
       "Gerado por `tools/gen-cards-doc.py` a partir de `data/cards.json`. Não edite à mão.", "",
       f"Total: {len(cards)} cartas.", ""]

for type_id, title in TYPES.items():
    group = [c for c in cards if c["type"] == type_id]
    if not group:
        continue
    out += [f"## {title} ({len(group)})", "",
            "| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |",
            "| --- | --- | --- | --- | --- | --- | --- |"]
    for c in group:
        out.append(f"| `{c['id']}` | {c['name']} | {RARITY[c['rarity']]} | {c['cost']} | {c['sell']} | "
                   f"{mods_text(c.get('mods'))} | {effect_text(c)} |")
    out.append("")

(ROOT / "docs" / "cards.md").write_text("\n".join(out), encoding="utf-8", newline="\n")
print(f"docs/cards.md: {len(cards)} cartas")
