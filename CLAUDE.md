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
- dotnet run --project server     # servidor local em ws://localhost:5000/ws (/health, /metrics)
- dotnet test server.tests        # 4 clientes WebSocket, reconexão, mão oculta
- docker compose up --build       # servidor + PostgreSQL (precisa do Docker Desktop)
- ./tools/simulate.sh 1000        # 1000 partidas IA×IA, imprime estatísticas
- ./tools/sync-data.sh            # copia data/ para o cliente Unity (Assets/_Project/Data/Resources)
- ./tools/sync-core.sh            # compila o core e copia a DLL para client/Assets/Plugins/BattleHunter
- Unity: abrir client/, cena Boot, Play. Modo offline não precisa do servidor.
- Unity em batch: Unity.exe -batchmode -projectPath client -runTests -testPlatform PlayMode
  (depois de sync-core.sh e sync-data.sh; editor em C:/Program Files/Unity/Hub/Editor/6000.6.4f1)

## Convenções
- C# 10, nullable habilitado, PascalCase público, _camelCase privado.
- Um tipo por arquivo. Namespaces espelham pastas (BattleHunter.Core.Rules).
- Testes: Given_When_Then no nome; um comportamento por teste.
- Commits pequenos e em português: "core: roubo de carta após dano PvP".
- Nunca edite client/Assets/_Project/Data nem client/Assets/Plugins/BattleHunter à mão; edite data/ ou core/ e sincronize.

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
