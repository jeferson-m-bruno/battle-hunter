# GDD — Battle Hunter-like (Unity Mobile + Servidor)

2026-10-05 · Jeferson Bruno

## Visão geral

Um jogo de tabuleiro tático competitivo, fiel às regras de Battle Hunter (PS1, 1999): até 4 caçadores entram num dungeon em grid, rolam dados para agir, coletam cartas em baús e disputam o tesouro-alvo da missão. Quem sai pela saída com o tesouro ganha a missão; os outros ganham só o que carregam.

**Pilares**

- Competição, não cooperação: atacar outro caçador para roubar o tesouro é regra central.
- Sorte controlada: o dado define quanto você faz no turno, as cartas definem como.
- Partidas curtas (10 a 20 min) com progressão persistente do caçador na guilda.

**Plataforma e modo**

- Cliente Unity (Android e iOS), retrato ou paisagem, controle por toque.
- Servidor autoritativo em C# (.NET) com o mesmo pacote de regras do cliente.
- Modos: online 2 a 4 jogadores (vagas vazias preenchidas por IA), solo offline contra IA.

**Fora do escopo da v1**

- Chat de voz, clãs, eventos ao vivo, passe de temporada.
- Monetização: decidir depois do núcleo jogável (ver pergunta aberta na seção de economia).

## Loop de partida

Cada partida é uma missão: 4 caçadores, um dungeon de 12×12 a 16×16 células, um tesouro-alvo escondido num baú e uma saída. A partida termina quando alguém sai com o tesouro ou quando todos os caçadores caem.

**Setup**

- Servidor gera o mapa (salas, corredores, baús, saída, pontos de spawn de monstros).
- Cada caçador entra numa sala inicial distinta, com a mão de cartas que trouxe da guilda (até 5).
- O tesouro-alvo vai para um baú aleatório; os demais baús recebem cartas da tabela de loot.
- Ordem de turno sorteada e fixa durante toda a partida.

**Turno do caçador**

- Rola 1d6 + bônus de Velocidade → pontos de ação (PA) do turno.
- Gasta PA em ações, em qualquer ordem, até zerar ou passar:Mover 1 célula ortogonal: 1 PA.
- Abrir baú adjacente: 2 PA, recebe a carta e ela vai para a mão.
- Atacar alvo adjacente (monstro ou caçador): 2 PA.
- Usar carta de item ou equipar: 1 PA (armadilhas custam 2 PA e são colocadas na célula atual).
- Sair pela célula de saída: 1 PA, encerra a participação do caçador.

PA não gasto é perdido.

Mão cheia (10 cartas) obriga descarte antes de pegar outra.

**Esclarecimentos de implementação (fatia 1)**

- Rolar o dado é uma ação explícita do jogador (`RollDice`), como no PS1; o turno nasce na fase "aguardando rolagem".
- Ao zerar o PA, o turno encerra automaticamente; `Pass` encerra com PA sobrando.
- Uma célula ocupada por outro caçador ativo bloqueia movimento (dois caçadores nunca dividem a célula).
- O limite de rodadas encerra a partida ao fim da última rodada (rodada 30 completa).
- A aleatoriedade usa um PRNG próprio (xorshift32): `System.Random` muda de algoritmo entre .NET 8 e Mono/Unity, o que quebraria o replay cliente↔servidor.

**Turno dos monstros**

Após os 4 caçadores, cada monstro vivo rola 1d6 de movimento e persegue o caçador mais próximo em linha de visão; se adjacente, ataca em vez de mover.

**Condições de fim**

| Condição | Resultado |
| --- | --- |
| Caçador sai com o tesouro-alvo | Vence a missão; recebe recompensa cheia |
| Caçador sai sem o tesouro | Mantém cartas e ouro coletados, sem recompensa de missão |
| Caçador reduzido a 0 PV | Cai; largam-se todas as cartas da mão na célula (podem ser pegas por 1 PA) |
| Limite de 30 rodadas | Missão falha para todos; quem está vivo mantém o que carrega |

Quem tiver o tesouro-alvo na mão aparece marcado no HUD de todos os jogadores — isso cria a caçada ao portador, que é o coração do jogo.

## Caçadores

Cada jogador tem um caçador persistente com 5 atributos; equipamentos e nível os modificam. Não há classes: a build vem das cartas equipadas.

| Atributo | Papel | Base (nv 1) | Ganho por nível |
| --- | --- | --- | --- |
| PV | Pontos de vida; 0 = cai | 20 | +3 |
| Ataque (ATQ) | Dano base físico | 4 | +1 |
| Defesa (DEF) | Reduz dano recebido | 2 | +1 a cada 2 nv |
| Velocidade (VEL) | Bônus de PA e iniciativa | 0 | +1 a cada 3 nv |
| Sorte (SOR) | Chance de crítico e qualidade de loot | 1 | +1 a cada 2 nv |

**Progressão**

- XP vem de monstros derrotados, baús abertos e missões concluídas; tabela de níveis de 1 a 30.
- Ao subir de nível o jogador distribui 1 ponto livre além dos ganhos automáticos.
- Equipamentos têm slots: arma, armadura, acessório. Cada slot aceita 1 carta; trocar custa 1 PA na partida.
- PV é restaurado por inteiro entre missões; cartas na mão persistem (até 5 levadas para a próxima).

**Aparência**

Cor, rosto e nome escolhidos na criação; sem efeito nas regras. Skins são a única candidata a cosmético pago.

## Sistema de cartas

Tudo que não é atributo é carta: equipamentos, consumíveis, armadilhas, ataques especiais e o próprio tesouro. Cartas são dados (JSON), nunca código, para que o Claude Code adicione dezenas sem tocar nas regras.

| Tipo | Como usa | Exemplos iniciais | Quantidade v1 |
| --- | --- | --- | --- |
| Arma | Equipa no slot arma; +ATQ, alguns com efeito | Adaga (+2), Espada (+4), Machado (+6, -1 VEL) | 10 |
| Armadura | Equipa no slot armadura; +DEF | Couro (+1), Cota (+3), Placas (+5, -1 VEL) | 8 |
| Acessório | Equipa no slot acessório; efeito passivo | Anel da Sorte (+2 SOR), Botas (+1 VEL) | 8 |
| Consumível | Usa e descarta; efeito imediato | Poção (+8 PV), Antídoto, Bomba (3 dano em área 3×3) | 12 |
| Armadilha | Coloca na célula; dispara em quem pisar | Fosso (4 dano), Rede (perde o próximo turno), Alarme (atrai monstros) | 6 |
| Ataque especial | Usa no lugar de um ataque normal; descarta | Golpe Duplo, Investida (2 células), Rasteira (derruba 2 cartas da mão do alvo) | 8 |
| Tesouro | Objetivo da missão; não equipa, só carrega | Dente de Kobold, Olho de Dragão, Coroa Perdida | 1 por missão |

**Regras de mão**

- Mão máxima na partida: 10 cartas. Ao pegar a 11ª, descarta uma antes.
- Até 5 cartas voltam para a guilda ao fim da missão; o resto vira ouro pelo valor de venda.
- Baús: 2 PA para abrir; a tabela de loot pesa raridade pela SOR de quem abre.
- Cartas na mão são ocultas; equipadas são visíveis a todos.

**Roubo**

Quando um caçador recebe dano de outro caçador e falha num teste de SOR (1d6 + SOR < 1d6 + SOR do atacante), o atacante rouba 1 carta aleatória da mão. O tesouro-alvo é sempre roubável e tem prioridade no sorteio.

**Esquema de dados da carta**

```json
{
  "id": "sword_iron",
  "name": "Espada de Ferro",
  "type": "weapon",
  "rarity": "common",
  "cost": 1,
  "sell": 20,
  "mods": { "atk": 4 },
  "effect": null
}
```

Efeitos ativos usam um id de efeito ("effect": "double_strike") resolvido num registro de efeitos no core; cada efeito é uma função pura com teste.

**Esclarecimentos de implementação (fatia 2)**

- Salas de canto ancoradas nos 4 cantos do grid (sem borda de parede); lado máximo 3 no 12×12, 4 no 14×14, 5 no 16×16. Salas extras em qualquer lugar, com 1 célula de folga entre salas.
- Células de baú não são pisáveis: abre-se de uma célula adjacente.
- "Mais distante da média dos spawns" = maior média das distâncias BFS a partir dos 4 spawns; candidatos a até 2 passos do máximo, fora das salas de spawn.
- Visão "2 passos" = distância de Manhattan ≤ 2, mais a sala atual inteira.
- Loot do baú: peso por raridade em `loot_tables.json` — comum 60, incomum 30 + 2×SOR, rara 10 + 3×SOR (SOR efetiva, com equipamentos).
- Descartar é gratuito (0 PA) e só é exigido ao tentar pegar a 11ª carta; o tesouro-alvo não pode ser descartado.
- Equipar com o slot ocupado devolve a carta anterior à mão (a mão não cresce).
- JSON é lido com Newtonsoft.Json no core: a Unity tem pacote oficial do mesmo assembly, então cliente e servidor leem `data/` do mesmo jeito.

## Combate

Ataques são resolvidos no servidor com um dado por lado; o cliente só anima o resultado. Uma ação de ataque custa 2 PA e atinge um alvo adjacente ortogonal.

**Fórmula de dano**

```latex
Dano = \max\big(1,\ (ATQ + 1d6) - (DEF + 1d6_{alvo})\big)
```

- Crítico: se o d6 do atacante for 6 e 1d6 ≤ SOR, o dano dobra.
- Esquiva: se o d6 do defensor for 6 e 1d6 ≤ SOR do defensor, dano zero.
- Dano mínimo 1 garante que ataques nunca sejam inúteis.

**PvE**

- Monstros usam a mesma fórmula com atributos fixos da tabela de monstros.
- Derrotar um monstro dá XP e, com 1d6 ≤ SOR, uma carta da tabela de loot do monstro.

**PvP**

- Mesma fórmula; após dano, teste de roubo (seção de cartas).
- Derrubar um caçador dá XP igual ao nível do alvo × 10 e larga toda a mão dele na célula.
- Não há punição por atacar jogadores: é a regra clássica e o motor da disputa.

**Cair e retornar**

- Caçador a 0 PV sai da partida; não há ressurreição na v1.
- Na guilda, PV volta ao máximo; perde-se o ouro não depositado (10% do carregado).

**Estados**

| Estado | Causa | Efeito | Duração |
| --- | --- | --- | --- |
| Veneno | Monstro ou armadilha | -2 PV no início do turno | 3 turnos ou Antídoto |
| Preso | Rede | Perde o próximo turno | 1 turno |
| Lento | Armadura pesada | -1 VEL | Enquanto equipado |
| Marcado | Carregar o tesouro-alvo | Visível a todos no mapa | Enquanto carrega |

**Esclarecimentos de implementação (fatia 3)**

- A fase dos monstros roda automaticamente quando o último caçador da rodada encerra o turno; monstros não têm SOR (nunca dão crítico nem esquivam) e não roubam cartas.
- Mímico: 15% dos baús comuns (nunca o baú-alvo), sorteado no setup; ao abrir, vira monstro na célula e ataca na hora (os 2 PA são gastos, nenhuma carta sai).
- Roubo só após dano de ataque (normal ou especial), nunca de bomba/armadilha; empate no teste de SOR protege a vítima.
- Caçador caído larga só a mão; o equipamento fica com ele. Veneno pode derrubar (sem XP para ninguém).
- Cartas ativas levam `effect` + `params` no JSON (ex.: `"effect": "heal", "params": {"amount": 8}`); um efeito pode servir a várias cartas.
- Ataques especiais custam 2 PA e usam a fórmula normal. Investida: alvo em linha reta a 2–3 células com caminho livre, o atacante avança e ataca com +2 ATQ. Rasteira derruba as cartas no chão da célula do alvo.
- Bomba: célula-alvo a até 3 passos; 3×3 atinge caçadores e monstros, inclusive quem joga. Arremessos (Pedra, Faca, Óleo) dão dano fixo sem dados.
- Armadilhas: uma por célula, nunca na saída; invisíveis; disparam em caçadores (menos o dono) e em monstros (Alarme e veneno só afetam caçadores). Alarme atrai monstros sem alvo à vista por 3 rodadas.
- Veneno tica (−2 PV) no início de cada um dos 3 turnos seguintes. Preso pula o próximo turno; em monstro, pula a próxima fase.
- Chefe: entra numa célula livre da sala da saída na rodada 15 ou ao pegar o tesouro-alvo (`BossRound` = 0 desliga); sopra a cada 3 ações próprias numa linha ortogonal de 3 células na direção do caçador mais próximo, parando em paredes; o sopro não pode ser esquivado. Loot rara garantida ao derrotá-lo.

## Mapa, monstros e chefe

O dungeon é gerado no servidor a partir de uma seed; o cliente recebe a seed e o layout, nunca gera sozinho. Visual isométrico 2D (sprites sobre grid), como o original.

**Geração**

- Grid de 12×12 (missão fácil) a 16×16 (difícil), células: chão, parede, baú, saída, spawn.
- Algoritmo: 5 a 8 salas retangulares ligadas por corredores em L; validação de conectividade por BFS.
- 4 spawns de caçador nos cantos; saída numa sala que não é spawn; 8 a 14 baús.
- Tesouro-alvo no baú mais distante (em passos) da média dos spawns, com variação de ±2.
- Monstros: 1 por sala sem spawn no início, mais 1 novo a cada 5 rodadas num spawn aleatório.

**Visão**

Cada caçador vê sua sala e as células a 2 passos; o resto fica em névoa. Baús já abertos ficam marcados para todos.

**Monstros**

| Monstro | PV | ATQ | DEF | XP | Loot | Comportamento |
| --- | --- | --- | --- | --- | --- | --- |
| Kobold | 8 | 3 | 1 | 10 | Comum | Persegue o mais próximo |
| Esqueleto | 12 | 4 | 2 | 15 | Comum | Guarda a sala, ataca quem entra |
| Aranha | 10 | 3 | 1 | 15 | Comum | Envenena ao acertar |
| Orc | 18 | 6 | 3 | 25 | Incomum | Persegue o mais ferido |
| Mímico | 15 | 5 | 4 | 30 | Raro | Parece baú; ataca ao ser aberto |
| Dragão Vermelho (chefe) | 60 | 10 | 6 | 150 | Raro garantido | Ver abaixo |

**Chefe**

- Aparece na rodada 15 ou quando o tesouro-alvo é pego, o que vier primeiro, na sala da saída.
- Sopro: ataque em linha de 3 células a cada 3 turnos, 8 de dano fixo, ignora DEF.
- Derrotá-lo é opcional; dá XP e uma carta rara, mas quem gastar turnos nele arrisca perder a corrida à saída.
- A IA dos caçadores avalia se vale enfrentar (seção de IA).

## Missões, guilda e economia

A guilda é o hub entre partidas: escolher missão, equipar, vender cartas e ver o ranking. Tudo é tela de menu, sem mundo aberto.

**Tipos de missão**

| Tipo | Grid | Rodadas | Chefe | Recompensa de vitória | Requisito |
| --- | --- | --- | --- | --- | --- |
| Caça (fácil) | 12×12 | 30 | Não | 100 ouro + 50 XP | Nenhum |
| Caça (normal) | 14×14 | 30 | Sim | 250 ouro + 120 XP | Nível 5 |
| Caça (difícil) | 16×16 | 25 | Sim, PV ×1.5 | 500 ouro + 250 XP + carta rara | Nível 12 |
| Ranqueada | 14×14 | 30 | Sim | Pontos de rank + 200 ouro | Nível 8 |

**Matchmaking**

- Fila por tipo de missão; faixa de nível ±4 para casuais, por rank para ranqueada.
- Após 30 s sem sala cheia, IA preenche as vagas.
- Sala de espera mostra equipamentos visíveis dos outros, como no original.

**Economia**

- Ouro vem de missões, venda de cartas e ouro solto em baús.
- Loja da guilda vende cartas comuns e incomuns com estoque rotativo diário; raras só em partida.
- Preço de compra = 3× valor de venda.
- Sem moeda premium na v1.

Pergunta aberta: monetização futura por skins apenas, ou também por slots extras de cartas levadas para a missão? A segunda afeta balanceamento e deve ser decidida antes do design de rank.

## IA

A IA roda no servidor (ou localmente no modo solo) sobre a mesma API de ações que um jogador humano usa; ela nunca vê a mão oculta dos outros nem células em névoa.

**Caçadores de IA: utilidade por ação**

A cada turno a IA lista as ações possíveis com o PA atual, pontua cada uma e executa a melhor, repetindo até zerar PA. Pesos iniciais, a calibrar:

| Situação | Ação preferida | Peso |
| --- | --- | --- |
| Tesouro-alvo na própria mão | Caminho mais curto à saída | 100 |
| Portador do tesouro a ≤ 3 passos e PV próprio > 50% | Perseguir e atacar o portador | 80 |
| Baú não aberto a ≤ 4 passos | Ir abrir | 50 |
| PV < 30% e tem Poção | Usar Poção | 70 |
| Monstro adjacente e PV > 40% | Atacar | 40 |
| Monstro adjacente e PV ≤ 40% | Afastar-se | 60 |
| Chefe presente e tesouro com outro | Evitar a sala da saída | 30 |
| Nada acima | Explorar célula em névoa mais próxima | 10 |

Três perfis ajustam os pesos: Agressivo (+30 em perseguir e atacar), Cauteloso (+30 em fugir e curar), Ganancioso (+30 em baús).

**Monstros**

- Comportamento por tipo é uma máquina de 3 estados: Ocioso, Perseguindo, Atacando.
- Transição por distância e linha de visão; o Esqueleto nunca sai da sala.
- Pathfinding A* no grid, custo 1 por célula, paredes bloqueiam.

**Testes de IA**

- Simulação de 1.000 partidas só com IA, sem renderização, para medir taxa de vitória por perfil e duração média.
- Meta de balanceamento: nenhum perfil acima de 35% de vitórias; duração média de 18 a 25 rodadas.

**Esclarecimentos de implementação (fatia 4)**

- O layout do mapa (paredes, baús, saída) é conhecido por todos desde o início; a névoa esconde criaturas, cartas no chão e os outros caçadores — o Marcado é sempre visível. A IA explora "névoa" = células que ainda não viu.
- A simulação usa 4 perfis: os três do GDD mais Equilibrado (pesos base, sem bônus); cada caçador entra com 2 cartas comuns aleatórias.
- Limiares da IA ficam em `data/ai_weights.json` junto dos pesos; o balanceamento registrado em `docs/balance.md` ajustou perseguição (≤ 6 passos, PV > 40%) e baús (≤ 5 passos).
- Esqueleto: persegue só dentro da própria sala; Orc: o caçador com menor % de PV à vista; Mímico revelado persegue como o Kobold.

## Arquitetura técnica

O núcleo de regras é uma biblioteca C# pura compilada tanto no cliente quanto no servidor; o cliente envia intenções, o servidor valida e devolve eventos, e o modo offline roda o mesmo núcleo localmente.

*[Diagrama: arquitetura · cliente, servidor e núcleo compartilhado]*

O servidor é a única fonte da verdade em partidas online; o cliente nunca rola dados nem decide dano.

**Rede**

- WebSocket com mensagens JSON (ou MessagePack se o tamanho pesar); uma sala = uma conexão por jogador.
- Reconexão: o servidor guarda o GameState e o jogador recebe o estado inteiro ao voltar; 60 s de tolerância antes de a IA assumir o caçador.
- Latência não é crítica: é turno a turno, com timer de 45 s por turno.

**Persistência**

- PostgreSQL: jogador, caçador, inventário de cartas, histórico de partidas, ranking.
- Login anônimo por dispositivo na v1; Google Play Games e Game Center depois.

**Hospedagem**

- Um container .NET por região; começa com uma única instância, salas em memória.
- Escala horizontal exige mover salas para Redis; deixar a interface IRoomStore pronta desde o início.

## Estrutura do repositório

Um monorepo com três projetos C#: o núcleo de regras (biblioteca pura), o servidor e o cliente Unity. O núcleo é referenciado pelos outros dois, então cliente e servidor nunca divergem nas regras.

```
battle-hunter/
  CLAUDE.md
  docs/
    GDD.md                 # este documento exportado
    cards.md               # catálogo de cartas gerado a partir do JSON
  core/                    # BattleHunter.Core (.NET Standard 2.1, sem Unity)
    Rules/                 # turno, PA, dado, combate, roubo, condições de fim
    Cards/                 # modelos, registro de efeitos, loot
    Map/                   # geração por seed, BFS, A*
    Ai/                    # utilidade dos caçadores, FSM dos monstros
    State/                 # GameState imutável + Reducer(state, action) -> state
    Serialization/         # DTOs compartilhados cliente/servidor
  core.tests/              # xUnit: regras, determinismo por seed, simulação em massa
  data/                    # JSON: cards, monsters, missions, loot_tables, levels
  server/                  # BattleHunter.Server (ASP.NET + WebSocket)
    Matchmaking/
    Rooms/                 # uma sala = um GameState + fila de ações
    Persistence/           # perfis, inventário, ranking (PostgreSQL)
    Auth/
  client/                  # projeto Unity (2022 LTS ou 6)
    Assets/
      _Project/
        Scripts/
          Net/             # cliente WebSocket, fila de eventos
          Presentation/    # grid isométrico, sprites, HUD, animação de dado
          Scenes/          # Boot, Guilda, Lobby, Partida, Resultado
          Offline/         # roda o core localmente contra IA
        Art/               # placeholders primeiro
        Data/              # cópia dos JSON de data/, sincronizada por script
  tools/
    sync-data.sh           # copia data/ para client/Assets/_Project/Data
    simulate.sh            # roda N partidas IA×IA e imprime estatísticas
```

**Decisões que travam a arquitetura**

- GameState é imutável e serializável; toda mudança passa por Reducer. Isso dá replay, teste e reconexão de graça.
- O cliente envia só intenções (MoveTo, OpenChest, Attack); o servidor valida, rola o dado e devolve eventos.
- Aleatoriedade vem de um IRandom com seed; em teste é determinístico.
- Nada em core/ referencia UnityEngine. O teste de build do core fora do Unity é obrigatório no CI.

## CLAUDE.md

Cole este arquivo na raiz do repositório. Ele é o que o Claude Code lê em toda sessão; mantenha curto e atualize quando uma decisão mudar.

```markdown
# Battle Hunter-like — guia para o Claude Code

## O que é
Jogo de tabuleiro tático competitivo (fiel a Battle Hunter, PS1): 4 caçadores,
grid isométrico, dado define PA, cartas definem ações, tesouro-alvo + saída.
Regras completas em docs/GDD.md. Em dúvida sobre regra: o GDD vence; se o GDD
não cobre, pergunte antes de inventar.

## Arquitetura (não negociável)
- core/ é .NET Standard 2.1 puro. NUNCA referencie UnityEngine lá.
- GameState imutável; toda mudança via Reducer(state, action) -> (state, events).
- Servidor é autoritativo. Cliente envia intenções, recebe eventos, só renderiza.
- Aleatoriedade só por IRandom com seed. Testes usam seed fixa.
- Cartas, monstros, missões e tabelas vivem em data/*.json, nunca em código.
  Novo efeito de carta = função pura em core/Cards/Effects + teste + entrada no JSON.

## Comandos
- dotnet test core.tests          # roda antes de qualquer commit
- dotnet run --project server     # servidor local em ws://localhost:5000
- ./tools/simulate.sh 1000        # 1000 partidas IA×IA, imprime estatísticas
- ./tools/sync-data.sh            # copia data/ para o cliente Unity
- Unity: abrir client/, cena Boot, Play. Modo offline não precisa do servidor.

## Convenções
- C# 10, nullable habilitado, PascalCase público, _camelCase privado.
- Um tipo por arquivo. Namespaces espelham pastas (BattleHunter.Core.Rules).
- Testes: Given_When_Then no nome; um comportamento por teste.
- Commits pequenos e em português: "core: roubo de carta após dano PvP".
- Nunca edite client/Assets/_Project/Data à mão; edite data/ e sincronize.

## Fluxo de trabalho
1. Leia a seção relevante do GDD antes de implementar.
2. Escreva o teste no core.tests primeiro quando for regra de jogo.
3. Implemente no core, depois exponha no servidor, por último no cliente.
4. Rode dotnet test e, se tocou em regra ou IA, ./tools/simulate.sh 200.
5. Se uma regra do GDD se mostrar inviável, proponha a mudança no chat
   e atualize docs/GDD.md no mesmo commit.

## Não faça
- Não adicione pacotes NuGet ou Unity sem dizer por quê.
- Não coloque lógica de regra em MonoBehaviour.
- Não use Random do .NET diretamente.
- Não altere balanceamento (números em data/) sem rodar a simulação antes e depois.
```

## Roadmap com Claude Code

O jogo é construído em sete fatias verticais, cada uma com um gate objetivo que o Claude Code consegue verificar sozinho antes de avançar; a primeira partida jogável na tela chega na fatia 5, com as regras já testadas.

*[Diagrama: roadmap · 7 fatias verticais com gate de saída]*

As fatias 1 a 4 não abrem o Unity: tudo roda em dotnet test e simulate.sh, onde o Claude Code é mais rápido e confiável.

**Como conduzir cada fatia**

- Abra a sessão com: "Leia CLAUDE.md e a seção X do GDD. Vamos implementar a fatia N."
- Peça primeiro um plano de arquivos e testes; aprove ou ajuste antes de ele codar.
- Deixe-o implementar em commits pequenos, rodando os testes a cada passo.
- No fim, peça: "Verifique o gate da fatia N e me mostre a evidência."
- Qualquer regra que mudar no caminho entra no GDD no mesmo commit.

**O que você precisa fornecer**

- Decisões de design quando ele perguntar (ele não deve inventar regras).
- Arte: placeholders geométricos até a fatia 5; depois, um pacote de sprites isométricos (Kenney, itch.io ou artista).
- Contas: Unity, loja Google Play e App Store, um host para o servidor (Fly.io ou Railway bastam no começo).

**Riscos**

| Risco | Sinal | Mitigação |
| --- | --- | --- |
| Regra de jogo no MonoBehaviour | Teste impossível sem Unity | CI compila o core fora do Unity |
| Balanceamento por intuição | Perfil de IA domina | Simulação obrigatória antes de mudar data/ |
| Servidor cedo demais | Semanas sem partida jogável | Online só na fatia 6, offline antes |
| Escopo crescendo | Fatia sem gate fechado | Nova ideia vai para o backlog, não para a fatia atual |
