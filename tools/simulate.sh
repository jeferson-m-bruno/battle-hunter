#!/usr/bin/env bash
# Roda N partidas IA×IA sem renderização e imprime estatísticas de balanceamento.
# Uso: ./tools/simulate.sh [N] [--seed S] [--mission easy|normal|hard]   (padrão: 1000 partidas, missão fácil)
# Sai com 0 quando a meta do GDD é atingida (nenhum perfil > 35% de vitórias, média de 18 a 25 rodadas).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
dotnet run -c Release --project "$ROOT/simulator" -- "$@"
