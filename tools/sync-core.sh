#!/usr/bin/env bash
# Compila o core (.NET Standard 2.1) em Release e copia a DLL para o cliente Unity.
# O Newtonsoft.Json NÃO é copiado: o Unity usa o pacote oficial com.unity.nuget.newtonsoft-json (mesmo assembly).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$ROOT/client/Assets/Plugins/BattleHunter"

dotnet build "$ROOT/core" -c Release --nologo -v q
mkdir -p "$DEST"
cp "$ROOT/core/bin/Release/netstandard2.1/BattleHunter.Core.dll" "$DEST/"
cp "$ROOT/core/bin/Release/netstandard2.1/BattleHunter.Core.xml" "$DEST/" 2>/dev/null || true
echo "BattleHunter.Core.dll copiada para client/Assets/Plugins/BattleHunter"
