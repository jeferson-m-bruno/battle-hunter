# Balanceamento

Resultados de `./tools/simulate.sh 1000` (4 caçadores IA: Agressivo, Cauteloso, Ganancioso, Equilibrado; 2 cartas comuns iniciais). Meta do GDD: nenhum perfil acima de 35% de vitórias e duração média de 18 a 25 rodadas.

Regra: toda mudança em `data/` entra com uma linha "antes" e uma "depois" aqui, nas mesmas seeds.

## 2026-10-06 — fatia 4: limiares da IA

Mudança em `data/ai_weights.json` (`thresholds`): `hunt_marked_max_steps` 3 → 6, `chest_max_steps` 4 → 5, `hunt_marked_min_hp_percent` 50 → 40. Motivo: a média de rodadas da missão fácil ficava em 17,3; mais perseguição ao portador do tesouro alonga a disputa sem desequilibrar os perfis.

| Rodada | Agressivo | Cauteloso | Ganancioso | Equilibrado | Tesouro extraído | Todos fora | Limite | Média de rodadas | Caídos/partida | Meta |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| antes · fácil · seeds 1–1000 | 14,1% | 16,0% | 16,3% | 19,7% | 66,1% | 4,8% | 29,1% | 17,3 | 0,97 | fora |
| antes · fácil · seeds 5001–6000 | 11,9% | 15,1% | 18,4% | 19,3% | 64,7% | 4,5% | 30,8% | 17,7 | 0,94 | fora |
| depois · fácil · seeds 1–1000 | 12,8% | 13,1% | 14,7% | 17,7% | 58,3% | 5,6% | 36,1% | 18,5 | 1,10 | ok |
| depois · fácil · seeds 1001–2000 | 11,0% | 13,9% | 16,6% | 16,1% | 57,6% | 5,5% | 36,9% | 18,7 | 1,16 | ok |
| depois · fácil · seeds 5001–6000 | 11,4% | 13,7% | 17,4% | 18,4% | 60,9% | 5,3% | 33,8% | 18,2 | 1,05 | ok |
| depois · normal · seeds 1–1000 | 7,7% | 8,9% | 11,3% | 15,4% | 43,3% | 16,7% | 40,0% | 21,0 | 1,76 | ok |
| depois · difícil · seeds 1–1000 | 5,2% | 9,4% | 12,1% | 16,7% | 43,4% | 6,9% | 49,7% | 19,2 | 1,38 | ok |

Observações:

- O Equilibrado vence um pouco mais que os outros em todas as missões: os bônus dos perfis tiram foco do tesouro. Dentro da meta; revisar quando houver jogadores humanos.
- Nas missões normal e difícil o limite de rodadas encerra 40–50% das partidas: o chefe na sala da saída e o mapa maior seguram a extração. Fica para a fatia 7 (tipos de missão) decidir se o limite sobe.
- Nenhuma partida abortada por excesso de ações; recusas de ação da IA abaixo de 0,1%.

## 2026-10-07 — fatia 7: ouro solto e XP por baú

Mudança: 25% dos baús comuns dão 15–40 de ouro em vez de carta (`GameConfig.ChestGoldPercent`), 5 XP por baú (`ChestXp`). Antes = linhas "depois" da seção anterior (mesmas seeds).

| Rodada | Agressivo | Cauteloso | Ganancioso | Equilibrado | Tesouro extraído | Todos fora | Limite | Média de rodadas | Caídos/partida | Meta |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| depois · fácil · seeds 1–1000 | 11,3% | 16,0% | 13,1% | 17,1% | 57,5% | 6,6% | 35,9% | 18,5 | 1,14 | ok |
| depois · fácil · seeds 5001–6000 | 9,9% | 14,5% | 16,0% | 18,7% | 59,1% | 6,3% | 34,6% | 18,3 | 1,12 | ok |
| depois · normal · seeds 1–1000 | 7,9% | 9,5% | 11,1% | 15,2% | 43,7% | 16,5% | 39,8% | 20,9 | 1,77 | ok |
| depois · difícil · seeds 1–1000 | 5,7% | 9,5% | 12,0% | 15,0% | 42,2% | 7,1% | 50,7% | 19,3 | 1,47 | ok |

Efeito pequeno: duração média igual (18,3–18,5 na fácil) e perfis ainda abaixo de 20%; dentro da meta. Mais ouro circulando alimenta a loja da guilda.
