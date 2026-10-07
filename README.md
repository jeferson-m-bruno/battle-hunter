# Battle Hunter-like

Jogo de tabuleiro tático competitivo para celular (Unity) com servidor autoritativo em .NET, fiel às regras de Battle Hunter (PS1, 1999).

- Regras: [docs/GDD.md](docs/GDD.md)
- Plano de desenvolvimento em fatias: [docs/PLANO.md](docs/PLANO.md)
- Guia para o Claude Code: [CLAUDE.md](CLAUDE.md)

## Estrutura

| Pasta | O que é |
| --- | --- |
| `core/` | BattleHunter.Core — regras puras (.NET Standard 2.1, sem Unity) |
| `core.tests/` | xUnit: regras, determinismo por seed, simulação |
| `server/` | BattleHunter.Server — ASP.NET + WebSocket: salas, matchmaking, reconexão, PostgreSQL opcional |
| `server.tests/` | Integração: 4 clientes WebSocket numa partida, reconexão, mão oculta |
| `client/` | Projeto Unity 6 (modo offline contra 3 IAs; cenas e UI montadas em código) |
| `data/` | Cartas, monstros, missões e tabelas em JSON |
| `tools/` | `sync-data.sh`, `sync-core.sh`, `simulate.sh`, `gen-cards-doc.py` |

## Comandos

```bash
dotnet test core.tests
```

```bash
./tools/simulate.sh 1000
```

Cliente Unity: rode `./tools/sync-core.sh` e `./tools/sync-data.sh`, abra `client/` no Unity 6, cena `Boot`, Play.

Online: `dotnet run --project server` e, no jogo, **Jogar online** apontando para `ws://localhost:5000/ws` (no celular, use o IP da máquina).
