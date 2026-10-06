# Catálogo de cartas

Gerado por `tools/gen-cards-doc.py` a partir de `data/cards.json`. Não edite à mão.

Total: 55 cartas.

## Armas (10)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `dagger_rusty` | Adaga Enferrujada | comum | 1 | 10 | +1 ATQ | — |
| `dagger` | Adaga | comum | 1 | 15 | +2 ATQ | — |
| `sword_short` | Espada Curta | comum | 1 | 20 | +3 ATQ | — |
| `sword_iron` | Espada de Ferro | comum | 1 | 20 | +4 ATQ | — |
| `mace` | Maça | incomum | 1 | 40 | +5 ATQ | — |
| `axe` | Machado | incomum | 1 | 50 | +6 ATQ, -1 VEL | — |
| `spear` | Lança | incomum | 1 | 45 | +4 ATQ, +1 VEL | — |
| `blade_lucky` | Lâmina da Sorte | incomum | 1 | 45 | +3 ATQ, +2 SOR | — |
| `greatsword` | Espadão | rara | 1 | 90 | +8 ATQ, -2 VEL | — |
| `fang_dragon` | Presa de Dragão | rara | 1 | 100 | +7 ATQ, +1 SOR | — |

## Armaduras (8)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `tunic` | Túnica | comum | 1 | 10 | +1 VEL | — |
| `leather` | Couro | comum | 1 | 15 | +1 DEF | — |
| `leather_studded` | Couro Cravejado | comum | 1 | 25 | +2 DEF | — |
| `chainmail` | Cota de Malha | incomum | 1 | 40 | +3 DEF | — |
| `scale` | Escamas | incomum | 1 | 50 | +4 DEF, -1 VEL | — |
| `vest_lucky` | Colete da Sorte | incomum | 1 | 45 | +2 DEF, +1 SOR | — |
| `plate` | Placas | rara | 1 | 80 | +5 DEF, -1 VEL | — |
| `scale_dragon` | Escama de Dragão | rara | 1 | 110 | +6 DEF, -1 VEL | — |

## Acessórios (8)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `boots` | Botas | comum | 1 | 20 | +1 VEL | — |
| `amulet_life` | Amuleto de Vida | comum | 1 | 25 | +5 PV | — |
| `belt_strength` | Cinto de Força | comum | 1 | 20 | +1 ATQ | — |
| `bracers` | Braçadeiras | comum | 1 | 20 | +1 DEF | — |
| `ring_luck` | Anel da Sorte | incomum | 1 | 40 | +2 SOR | — |
| `charm_fox` | Amuleto da Raposa | incomum | 1 | 45 | +1 VEL, +1 SOR | — |
| `cloak_shadow` | Manto das Sombras | rara | 1 | 80 | +2 VEL | — |
| `crown_hunter` | Coroa do Caçador | rara | 1 | 100 | +1 ATQ, +1 DEF, +1 SOR | — |

## Consumíveis (12)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `bread` | Pão | comum | 1 | 5 | — | `heal` (amount=4) |
| `potion` | Poção | comum | 1 | 15 | — | `heal` (amount=8) |
| `potion_large` | Poção Grande | incomum | 1 | 35 | — | `heal` (amount=15) |
| `elixir` | Elixir | rara | 1 | 80 | — | `heal` (amount=99) |
| `antidote` | Antídoto | comum | 1 | 10 | — | `cure_poison` |
| `bomb` | Bomba | incomum | 1 | 30 | — | `bomb` (damage=3, range=3) |
| `bomb_large` | Bomba Grande | rara | 1 | 70 | — | `bomb` (damage=5, range=3) |
| `adrenaline` | Adrenalina | incomum | 1 | 30 | — | `gain_ap` (amount=2) |
| `adrenaline_strong` | Adrenalina Forte | rara | 1 | 70 | — | `gain_ap` (amount=4) |
| `rock` | Pedra | comum | 1 | 5 | — | `throw` (damage=2, range=3) |
| `throwing_knife` | Faca de Arremesso | incomum | 1 | 25 | — | `throw` (damage=4, range=3) |
| `oil_flask` | Frasco de Óleo | comum | 1 | 15 | — | `throw` (damage=3, range=2) |

## Armadilhas (6)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `trap_pit` | Fosso | comum | 2 | 15 | — | `trap` (damage=4) |
| `trap_pit_deep` | Fosso Profundo | rara | 2 | 60 | — | `trap` (damage=7) |
| `trap_net` | Rede | incomum | 2 | 25 | — | `trap` (net=1) |
| `trap_alarm` | Alarme | comum | 2 | 10 | — | `trap` (alarm=3) |
| `trap_spikes` | Espinhos Venenosos | incomum | 2 | 30 | — | `trap` (damage=2, poison=1) |
| `trap_bear` | Mandíbula de Ferro | rara | 2 | 65 | — | `trap` (damage=5, net=1) |

## Ataques especiais (8)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `double_strike` | Golpe Duplo | incomum | 2 | 40 | — | `double_strike` |
| `charge` | Investida | incomum | 2 | 35 | — | `charge` (range=3, bonus_atk=2) |
| `trip` | Rasteira | comum | 2 | 20 | — | `trip` (cards=2) |
| `trip_brutal` | Rasteira Brutal | rara | 2 | 60 | — | `trip` (cards=4) |
| `power_strike` | Golpe Poderoso | comum | 2 | 20 | — | `strike` (bonus_atk=4) |
| `precise_strike` | Golpe Preciso | incomum | 2 | 35 | — | `strike` (bonus_atk=1, no_dodge=1) |
| `whirlwind` | Redemoinho | rara | 2 | 70 | — | `whirlwind` |
| `life_steal` | Drenar | incomum | 2 | 40 | — | `life_steal` |

## Tesouros (3)

| Id | Nome | Raridade | Custo (PA) | Venda | Modificadores | Efeito |
| --- | --- | --- | --- | --- | --- | --- |
| `treasure_kobold_fang` | Dente de Kobold | rara | 0 | 100 | — | — |
| `treasure_dragon_eye` | Olho de Dragão | rara | 0 | 250 | — | — |
| `treasure_lost_crown` | Coroa Perdida | rara | 0 | 500 | — | — |
