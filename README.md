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
| `server/` | BattleHunter.Server — ASP.NET + WebSocket (fatia 6) |
| `client/` | Projeto Unity (fatia 5) |
| `data/` | Cartas, monstros, missões e tabelas em JSON |
| `tools/` | `sync-data.sh`, `simulate.sh` |

## Comandos

```bash
dotnet test core.tests
```
