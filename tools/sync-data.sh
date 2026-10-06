#!/usr/bin/env bash
# Copia data/ para o cliente Unity. Nunca edite client/Assets/_Project/Data à mão.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/data"
DEST="$ROOT/client/Assets/_Project/Data"

mkdir -p "$DEST"
cp "$SRC"/*.json "$DEST"/
echo "data/ sincronizado para client/Assets/_Project/Data ($(ls "$SRC"/*.json | wc -l) arquivos)"
