# Plano de desenvolvimento — Battle Hunter-like

2026-10-06 · Jeferson Bruno

## Como usar este plano

O jogo é construído em oito etapas (uma fundação mais as sete fatias verticais do GDD), cada uma com um gate objetivo que fecha a etapa antes da seguinte. A primeira partida jogável na tela chega na fatia 5; as fatias 0 a 4 rodam só em `dotnet test` e `simulate.sh`, sem abrir o Unity.

Ritual de cada fatia com o Claude Code:

1. Abrir a sessão com: "Leia CLAUDE.md e a seção X do GDD. Vamos implementar a fatia N."
2. Pedir primeiro um plano de arquivos e testes; aprovar ou ajustar antes de codar.
3. Implementar em commits pequenos, rodando `dotnet test` a cada passo.
4. Fechar com: "Verifique o gate da fatia N e me mostre a evidência."
5. Qualquer regra que mudar no caminho entra em `docs/GDD.md` no mesmo commit.

Marque as caixas conforme concluir; o gate de cada fatia fica no fim da seção. Fonte das regras: [GDD — Battle Hunter-like](https://claude.ai/artifact/TpPnryj9LfUbwRxCkUhYg4).

## Fatia 0 — Fundação do repositório

Antes de qualquer regra: o monorepo, o CLAUDE.md e o CI que compila o core fora do Unity. Sem isso, as fatias seguintes não têm onde fechar o gate.

**Repositório e documentação**

- [x] Criar o repositório `battle-hunter/` com `git init` e `.gitignore` para .NET e Unity
- [x] Colar o CLAUDE.md do GDD na raiz
- [x] Exportar o GDD para `docs/GDD.md`
- [x] Criar `docs/cards.md` vazio (será gerado a partir do JSON na fatia 2)

**Solução .NET**

- [x] Criar `core/` como BattleHunter.Core (.NET Standard 2.1, nullable habilitado, C# 10)
- [x] Criar `core.tests/` com xUnit referenciando o core
- [x] Criar `server/` como BattleHunter.Server (ASP.NET) referenciando o core, ainda vazio
- [x] Criar `BattleHunter.sln` agrupando os três projetos
- [x] Criar as pastas `Rules/`, `Cards/`, `Map/`, `Ai/`, `State/`, `Serialization/` no core com namespaces espelhando pastas
- [x] Adicionar um teste trivial que compila e passa, para validar a cadeia

**Dados e ferramentas**

- [x] Criar `data/` com `cards.json`, `monsters.json`, `missions.json`, `loot_tables.json`, `levels.json` vazios (arrays)
- [x] Criar `tools/sync-data.sh` que copia `data/` para `client/Assets/_Project/Data`
- [x] Criar `tools/simulate.sh` como stub que imprime "não implementado" (vira real na fatia 4)

**CI**

- [x] Configurar CI (GitHub Actions) que roda `dotnet build core` e `dotnet test core.tests` a cada push
- [x] Adicionar verificação no CI que falha se `core/` referenciar `UnityEngine`

**Gate da fatia 0**

- [x] CI verde com o teste trivial, core compilando fora do Unity

## Fatia 1 — Núcleo de turno

Entrega o esqueleto de uma partida: estado imutável, reducer, dado com seed, pontos de ação e movimento num grid fixo. Ainda sem mapa gerado, cartas ou combate.

**Estado e reducer**

- [x] Definir `GameState` imutável e serializável: rodada, ordem de turno, caçadores, grid, PA do turno atual
- [x] Definir `Hunter` com os 5 atributos (PV 20, ATQ 4, DEF 2, VEL 0, SOR 1 na base) e posição
- [x] Definir as ações (intenções) como tipos: `RollDice`, `MoveTo`, `Pass`, `Exit`
- [x] Definir os eventos como tipos: `DiceRolled`, `HunterMoved`, `TurnEnded`, `HunterExited`, `GameEnded`
- [x] Implementar `Reducer.Apply(state, action, random) -> (state, events)` como função pura
- [x] Rejeitar ações inválidas (fora do turno, PA insuficiente, célula bloqueada) com evento de erro, sem mudar o estado

**Aleatoriedade**

- [x] Criar a interface `IRandom` com `NextD6()`
- [x] Implementar `SeededRandom` determinístico por seed
- [x] Garantir que nada no core use `System.Random` diretamente

**Turno**

- [x] Rolar 1d6 + VEL no início do turno e creditar como PA
- [x] Mover 1 célula ortogonal por 1 PA; paredes bloqueiam
- [x] Passar o turno descartando o PA restante
- [x] Sair pela célula de saída por 1 PA, encerrando a participação do caçador
- [x] Ordem de turno sorteada no setup e fixa durante a partida
- [x] Contar rodadas; encerrar a partida no limite configurado (30 por padrão)

**Fim de partida (parcial)**

- [x] Encerrar quando todos os caçadores saíram ou caíram
- [x] Encerrar no limite de rodadas como missão falha para todos

**Testes (core.tests)**

- [x] Dada uma seed, dois reducers produzem estados idênticos (determinismo)
- [x] Mover além do PA disponível é rejeitado
- [x] Mover para parede é rejeitado
- [x] Passar o turno zera o PA e avança para o próximo caçador
- [x] Partida termina na rodada 30
- [x] GameState serializa e desserializa sem perda (JSON)

**Gate da fatia 1**

- [x] `dotnet test core.tests` verde no CI

## Fatia 2 — Mapa e baús

O dungeon passa a ser gerado por seed e as cartas entram como dados em JSON. Ao fim, um caçador anda num mapa real, abre baús e gerencia a mão.

**Geração do mapa (core/Map)**

- [x] Definir tipos de célula: chão, parede, baú, saída, spawn de monstro
- [x] Gerar 5 a 8 salas retangulares em grid de 12×12 a 16×16 a partir da seed
- [x] Ligar salas por corredores em L
- [x] Validar conectividade por BFS; regenerar se falhar
- [x] Colocar 4 spawns de caçador nos cantos, em salas distintas
- [x] Colocar a saída numa sala que não é spawn
- [x] Distribuir 8 a 14 baús
- [x] Colocar o tesouro-alvo no baú mais distante (em passos BFS) da média dos spawns, com variação de ±2
- [x] Marcar 1 spawn de monstro por sala sem caçador (os monstros em si entram na fatia 3)
- [x] Serializar o layout gerado para o cliente receber seed + layout

**Visão**

- [x] Calcular células visíveis: a sala atual mais 2 passos
- [x] Manter névoa para o resto; baús abertos ficam marcados para todos

**Cartas como dados (core/Cards + data/)**

- [x] Definir o modelo `Card` conforme o esquema do GDD: id, name, type, rarity, cost, sell, mods, effect
- [x] Carregar `data/cards.json` no core sem dependência de Unity
- [x] Escrever as cartas iniciais em JSON: 10 armas, 8 armaduras, 8 acessórios, 3 tesouros (consumíveis, armadilhas e ataques especiais movidos para a fatia 3, junto dos efeitos)
- [x] Criar o registro de efeitos (`effect` id → função pura) com os efeitos de equipamento (mods em ATQ/DEF/VEL/SOR)
- [x] Definir `data/loot_tables.json` com pesos por raridade
- [x] Sortear loot ponderando raridade pela SOR de quem abre
- [x] Gerar `docs/cards.md` a partir do JSON por script

**Mão e baús (core/Rules)**

- [x] Ação `OpenChest`: 2 PA, baú adjacente, carta vai para a mão
- [x] Mão máxima de 10: ao pegar a 11ª, exigir descarte antes (ação `Discard`)
- [x] Ação `Equip`: 1 PA, slots arma/armadura/acessório, substitui a carta do slot
- [x] Cartas na mão ocultas aos outros; equipadas visíveis
- [x] Estado Marcado: quem carrega o tesouro-alvo fica visível a todos
- [x] Fim de partida: sair com o tesouro-alvo vence a missão

**Testes**

- [x] Mesma seed gera o mesmo mapa
- [x] 1.000 mapas gerados são todos conexos
- [x] Tesouro-alvo nunca cai numa sala de spawn
- [x] Abrir baú com 1 PA é rejeitado
- [x] 11ª carta exige descarte
- [x] Equipar Machado aplica +6 ATQ e -1 VEL
- [x] Todas as cartas do JSON têm `effect` nulo ou registrado

**Gate da fatia 2**

- [x] Teste de 1.000 mapas conexos verde no CI

## Fatia 3 — Combate e roubo

A fórmula de dano, monstros, PvP com roubo de carta e os estados. Ao fim, todas as regras de combate do GDD estão cobertas por teste.

**Fórmula de dano**

- [x] Ação `Attack`: 2 PA, alvo adjacente ortogonal (monstro ou caçador)
- [x] Dano = max(1, (ATQ + 1d6) − (DEF + 1d6 do alvo))
- [x] Crítico: d6 do atacante = 6 e 1d6 ≤ SOR → dano dobra
- [x] Esquiva: d6 do defensor = 6 e 1d6 ≤ SOR do defensor → dano zero
- [x] Eventos `AttackResolved` com os dados rolados, para o cliente animar

**Monstros (core + data/monsters.json)**

- [x] Modelo `Monster` com PV, ATQ, DEF, XP, tabela de loot e comportamento
- [x] Escrever `monsters.json`: Kobold, Esqueleto, Aranha, Orc, Mímico, Dragão Vermelho
- [x] Spawn inicial: 1 monstro por sala sem caçador
- [x] Spawn contínuo: +1 monstro a cada 5 rodadas num spawn aleatório
- [x] Turno dos monstros após os 4 caçadores: 1d6 de movimento, persegue o mais próximo em linha de visão, ataca se adjacente (movimento simples; a FSM completa entra na fatia 4)
- [x] Derrotar monstro dá XP e, com 1d6 ≤ SOR, uma carta da tabela de loot dele
- [x] Mímico: aparece como baú e ataca ao ser aberto
- [x] Aranha envenena ao acertar

**Chefe**

- [x] Dragão aparece na rodada 15 ou quando o tesouro-alvo é pego, na sala da saída
- [x] Sopro: linha de 3 células a cada 3 turnos, 8 de dano fixo, ignora DEF
- [x] Derrotá-lo dá 150 XP e uma carta rara garantida

**PvP e roubo**

- [x] Após dano de caçador em caçador: teste de roubo (1d6 + SOR do alvo < 1d6 + SOR do atacante)
- [x] Roubo pega 1 carta aleatória da mão; o tesouro-alvo tem prioridade no sorteio
- [x] Derrubar caçador dá XP = nível do alvo × 10
- [x] Caçador a 0 PV cai: toda a mão fica na célula, pegável por 1 PA
- [x] Sem ressurreição na v1

**Cartas ativas**

- [x] Ação `UseCard`: 1 PA para consumível, 2 PA para armadilha na célula atual
- [x] Efeitos de consumível: Poção (+8 PV), Antídoto, Bomba (3 de dano em área 3×3)
- [x] Armadilhas: Fosso (4 de dano), Rede (perde o próximo turno), Alarme (atrai monstros); disparam ao pisar
- [x] Ataques especiais no lugar do ataque normal, descartam: Golpe Duplo, Investida (2 células), Rasteira (derruba 2 cartas da mão do alvo)
- [x] Escrever em `cards.json`: 12 consumíveis, 6 armadilhas, 8 ataques especiais (movido da fatia 2)
- [x] Cada efeito é uma função pura em `core/Cards/Effects` com teste próprio

**Estados**

- [x] Veneno: −2 PV no início do turno por 3 turnos ou até Antídoto
- [x] Preso: perde o próximo turno
- [x] Lento: −1 VEL enquanto armadura pesada equipada
- [x] Marcado: já feito na fatia 2; confirmar que persiste após roubo

**Testes**

- [x] Dano mínimo é 1
- [x] Crítico dobra o dano com seed que força 6 e SOR suficiente
- [x] Esquiva zera o dano
- [x] Roubo transfere o tesouro-alvo com prioridade
- [x] Caçador derrubado larga a mão inteira na célula
- [x] Sopro do chefe ignora DEF e acerta as 3 células
- [x] Veneno expira após 3 turnos
- [x] Um teste por efeito de carta registrado

**Gate da fatia 3**

- [x] Cada regra das seções Combate e Sistema de cartas do GDD tem ao menos um teste nomeado Given_When_Then

## Fatia 4 — IA e simulação

Caçadores e monstros controlados por IA sobre a mesma API de ações de um jogador, mais o simulador em massa que vira a ferramenta de balanceamento do projeto.

**Pathfinding (core/Map)**

- [x] A* no grid com custo 1 por célula; paredes bloqueiam
- [x] Linha de visão entre duas células
- [x] Distância em passos entre células (reusa o BFS da fatia 2)

**Monstros: FSM (core/Ai)**

- [x] Três estados: Ocioso, Perseguindo, Atacando
- [x] Transição por distância e linha de visão
- [x] Kobold persegue o mais próximo; Orc persegue o mais ferido
- [x] Esqueleto nunca sai da sala; ataca quem entra
- [x] Substituir o movimento simples da fatia 3 pela FSM

**Caçadores: utilidade por ação (core/Ai)**

- [x] Listar ações possíveis com o PA atual, pontuar cada uma, executar a melhor, repetir até zerar PA
- [x] A IA só vê o que um jogador veria: nunca a mão oculta dos outros nem células em névoa
- [x] Pesos iniciais do GDD: tesouro na mão → saída (100); portador a ≤ 3 passos e PV > 50% → perseguir (80); PV < 30% com Poção → usar (70); monstro adjacente e PV ≤ 40% → afastar (60); baú a ≤ 4 passos → abrir (50); monstro adjacente e PV > 40% → atacar (40); chefe presente e tesouro com outro → evitar sala da saída (30); explorar névoa (10)
- [x] Pesos em `data/ai_weights.json`, não em código
- [x] Três perfis: Agressivo (+30 perseguir/atacar), Cauteloso (+30 fugir/curar), Ganancioso (+30 baús)
- [x] Avaliação de enfrentar o chefe ou correr para a saída

**Simulador (tools/simulate.sh)**

- [x] Projeto console `BattleHunter.Simulator` que roda N partidas IA×IA sem renderização
- [x] Imprimir taxa de vitória por perfil, duração média em rodadas, quedas, partidas por tempo
- [x] `tools/simulate.sh 1000` chama o simulador com seed inicial configurável
- [x] Rodar 1.000 partidas em menos de 1 minuto (meta de performance para o CI)

**Balanceamento**

- [x] Rodar 1.000 partidas e registrar o baseline em `docs/balance.md`
- [x] Ajustar pesos e números de `data/` até nenhum perfil passar de 35% de vitórias
- [x] Ajustar até a duração média ficar entre 18 e 25 rodadas
- [x] Registrar cada mudança de balanceamento com a simulação antes e depois

**Testes**

- [x] A* encontra o caminho mais curto num mapa conhecido
- [x] IA com tesouro na mão sempre escolhe ir para a saída
- [x] IA nunca age sobre uma célula em névoa
- [x] Esqueleto não sai da sala em 100 turnos simulados
- [x] Simulação de 200 partidas termina sem exceção (teste de fumaça no CI)

**Gate da fatia 4**

- [x] `./tools/simulate.sh 1000`: nenhum perfil acima de 35% de vitórias e duração média de 18 a 25 rodadas

## Fatia 5 — Cliente Unity offline

A primeira partida jogável na tela: o core roda localmente contra 3 IAs, com placeholders geométricos. Nenhuma regra entra em MonoBehaviour.

**Projeto Unity**

- [x] Instalar o Unity Hub e o editor (Unity 6 — 6000.6.4f1); módulo Android pendente de instalação pelo Hub
- [x] Criar `client/` como projeto 2D; configurar retrato e paisagem
- [x] Referenciar o core compilado (DLL .NET Standard 2.1) em `Assets/Plugins`
- [x] Rodar `tools/sync-data.sh` e carregar os JSON de `Assets/_Project/Data`
- [x] Criar as cenas Boot, Guilda, Lobby, Partida, Resultado (Guilda e Lobby como stubs)

**Offline (Scripts/Offline)**

- [x] `OfflineGameHost` que instancia o core, 1 caçador humano e 3 IAs com perfis sorteados
- [x] Fila de eventos do reducer para a camada de apresentação
- [x] Turno da IA e dos monstros executados com pequeno atraso para o jogador acompanhar

**Apresentação (Scripts/Presentation)**

- [x] Grid isométrico 2D com sprites placeholder para chão, parede, baú, saída
- [x] Sprites placeholder para caçadores (cor escolhida) e monstros (forma por tipo)
- [x] Névoa de guerra: células fora da visão escurecidas
- [x] Animação do dado no início do turno
- [x] Animação de movimento, ataque e dano com os números do evento
- [x] HUD: PV, PA restante, rodada, ordem de turno, marcador do portador do tesouro
- [x] Mão de cartas em painel inferior; equipados visíveis no caçador
- [x] Timer de turno de 45 s com passagem automática

**Controle por toque**

- [x] Tocar numa célula alcançável move (caminho destacado com custo em PA)
- [x] Tocar num baú adjacente abre; tocar num alvo adjacente ataca
- [x] Tocar numa carta mostra ações: usar, equipar, descartar
- [x] Botão Passar e botão Sair quando na célula de saída
- [x] Modal de descarte obrigatório na 11ª carta

**Tela de resultado**

- [x] Vencedor da missão, cartas mantidas (até 5), ouro e XP ganhos
- [x] Botão Jogar de novo com nova seed

**Verificação**

- [ ] Build Android instalado num celular real
- [x] CI continua compilando o core fora do Unity (nenhum `using UnityEngine` em core/)

**Gate da fatia 5**

- [ ] Uma partida completa contra 3 IAs, do dado inicial à tela de resultado, num celular

## Fatia 6 — Servidor online

O servidor .NET vira a única fonte da verdade: recebe intenções, roda o mesmo core, devolve eventos. Ao fim, 4 celulares jogam a mesma partida.

**Salas (server/Rooms)**

- [x] `Room` = um GameState + fila de ações processada em ordem
- [x] Interface `IRoomStore` com implementação em memória (Redis fica para depois)
- [x] Servidor rola os dados e valida cada intenção; cliente nunca decide dano
- [x] Timer de 45 s por turno; passa automaticamente ao expirar
- [x] Eventos enviados a todos; mão oculta filtrada por destinatário
- [x] Vagas vazias e jogadores ausentes assumidos pela IA da fatia 4

**Rede (WebSocket)**

- [x] Endpoint WebSocket em ASP.NET em `ws://localhost:5000`
- [x] Mensagens JSON com os DTOs de `core/Serialization`; avaliar MessagePack se o tamanho pesar
- [x] Uma conexão por jogador por sala
- [x] Reconexão: GameState inteiro reenviado ao voltar; 60 s de tolerância antes de a IA assumir
- [x] Heartbeat e detecção de desconexão

**Matchmaking**

- [x] Fila por tipo de missão
- [x] Faixa de nível ±4 para casuais; por rank para ranqueada
- [x] Após 30 s sem sala cheia, IA preenche as vagas
- [ ] Sala de espera exibindo equipamentos visíveis dos outros

**Autenticação e persistência**

- [x] Login anônimo por id de dispositivo (Google Play Games e Game Center ficam para depois)
- [ ] PostgreSQL via Docker Compose para desenvolvimento local (compose e PostgresPlayerStore prontos; validar com o Docker Desktop ligado)
- [x] Tabelas: jogador, caçador, inventário de cartas, histórico de partidas, ranking
- [x] Migrações versionadas
- [x] Salvar resultado da partida: cartas mantidas, ouro, XP

**Cliente (Scripts/Net)**

- [x] Cliente WebSocket com fila de eventos reaproveitando a apresentação da fatia 5
- [x] Tela de Lobby: escolher missão, entrar na fila, sala de espera
- [x] Tratamento de queda e reconexão com reconstrução do estado

**Hospedagem**

- [x] Dockerfile do servidor
- [ ] Deploy de uma instância no Fly.io (fly.toml pronto, região GRU) com PostgreSQL gerenciado
- [x] Log estruturado e métrica básica: salas ativas, partidas por hora

**Testes**

- [x] Teste de integração: 4 clientes simulados completam uma partida pelo WebSocket
- [x] Intenção fora do turno é rejeitada pelo servidor
- [x] Reconexão recebe o estado idêntico ao da sala
- [x] Cliente nunca recebe a mão oculta de outro jogador

**Gate da fatia 6**

- [ ] 4 celulares numa mesma partida até a tela de resultado, com o servidor hospedado

## Fatia 7 — Guilda e progressão

O hub entre partidas e tudo que faz o caçador persistir: níveis, loja, tipos de missão, ranking e a arte final. Fecha com um beta fechado.

**Progressão (core + data/levels.json)**

- [x] Tabela de níveis 1 a 30 em `levels.json`
- [x] Ganhos automáticos por nível: +3 PV, +1 ATQ, +1 DEF a cada 2, +1 VEL a cada 3, +1 SOR a cada 2
- [x] 1 ponto livre por nível distribuído pelo jogador
- [x] XP de monstros, baús e missões somado ao fim da partida
- [x] PV restaurado entre missões; até 5 cartas levadas para a próxima
- [x] Ao cair: perde 10% do ouro não depositado

**Missões (data/missions.json)**

- [x] Caça fácil: 12×12, 30 rodadas, sem chefe, 100 ouro + 50 XP, sem requisito
- [x] Caça normal: 14×14, 30 rodadas, chefe, 250 ouro + 120 XP, nível 5
- [x] Caça difícil: 16×16, 25 rodadas, chefe com PV ×1.5, 500 ouro + 250 XP + carta rara, nível 12
- [x] Ranqueada: 14×14, 30 rodadas, chefe, pontos de rank + 200 ouro, nível 8
- [x] Sistema de pontos de rank e temporada simples

**Economia e loja**

- [x] Ouro de missões, venda de cartas e ouro solto em baús
- [x] Cartas além das 5 mantidas viram ouro pelo valor de venda
- [x] Loja da guilda com estoque rotativo diário de comuns e incomuns
- [x] Preço de compra = 3× valor de venda
- [x] Depósito de ouro na guilda

**Telas (Scenes/Guilda)**

- [x] Criação do caçador: cor, rosto e nome
- [x] Guilda: escolher missão, equipar slots, vender cartas, ver ranking
- [x] Tela de subida de nível com distribuição do ponto livre
- [x] Ranking global e por temporada

**Arte final**

- [ ] Escolher pacote de sprites isométricos (Kenney, itch.io ou artista) — `SpriteCatalog` pronto: basta colocar os PNG em `client/Assets/_Project/Art/Resources/Art/` com os nomes do README
- [ ] Substituir placeholders: tiles, caçadores, monstros, chefe, cartas, HUD
- [ ] Ícones e arte de loja
- [x] Som básico: dado, ataque, baú, vitória

**Publicação**

- [ ] Contas de desenvolvedor Google Play e App Store
- [ ] Builds de teste: faixa interna no Google Play e TestFlight
- [ ] Política de privacidade e termos mínimos

**Testes**

- [x] Subir de nível aplica os ganhos corretos da tabela
- [x] Missão com requisito de nível é bloqueada abaixo dele
- [x] Venda e compra respeitam o fator 3×
- [x] Simulação de 1.000 partidas por tipo de missão dentro das metas de balanceamento

**Gate da fatia 7**

- [ ] Beta fechado com testadores externos jogando partidas online completas

## Decisões pendentes e riscos

Decisões que o GDD deixa abertas e que travam uma fatia específica. O Claude Code não deve inventar regras: cada uma precisa de resposta antes da fatia indicada.

- [x] Monetização: só skins (campo `Skin` no perfil); nada de slots pagos
- [x] Versão do Unity: Unity 6 (6000.6.4f1)
- [x] Formato de rede: JSON (snapshot de ~10–20 KB por atualização; MessagePack só se pesar no 4G)
- [x] Host do servidor: Fly.io (fly.toml na raiz)
- [ ] Fonte da arte: pacote pronto ou artista? Decidir antes da fatia 7

| Risco | Sinal | Mitigação |
| --- | --- | --- |
| Regra de jogo no MonoBehaviour | Teste impossível sem Unity | CI compila o core fora do Unity (fatia 0) |
| Balanceamento por intuição | Um perfil de IA domina | Simulação obrigatória antes e depois de mudar `data/` (fatia 4) |
| Servidor cedo demais | Semanas sem partida jogável | Online só na fatia 6; offline antes |
| Escopo crescendo | Fatia sem gate fechado | Nova ideia vai para o backlog, não para a fatia atual |
| Unity não instalado na máquina de desenvolvimento | Fatia 5 não começa | Instalar Hub e editor durante a fatia 4 |
