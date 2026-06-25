#!/usr/bin/env bash
# Local build + mod package under dist/SnapBounty/ (folder, for manual deploy).
#
# Usage:
#   ./build.sh                                  # uses local macOS Steam installation
#   GAME_ROOT=/opt/7dtd ./build.sh              # custom installation (Linux/server)
#   MANAGED_DIR="/path/to/Managed" ./build.sh   # provide the Managed folder directly
#   DEPLOY_DIR="/path/to/7 Days To Die/Mods" ./build.sh   # also copy into the Mods directory
set -euo pipefail

cd "$(dirname "$0")"
ASM="SnapBounty"

ARGS=(-c Release)
[ -n "${GAME_ROOT:-}" ]   && ARGS+=(-p:GameRoot="$GAME_ROOT")
[ -n "${MANAGED_DIR:-}" ] && ARGS+=(-p:ManagedDir="$MANAGED_DIR")

echo ">> dotnet build src/$ASM.csproj ${ARGS[*]}"
dotnet build "src/$ASM.csproj" "${ARGS[@]}"

OUT="dist/$ASM"
rm -rf "$OUT"
mkdir -p "$OUT/Config"
cp "bin/Release/$ASM.dll" "$OUT/"
[ -f "bin/Release/$ASM.pdb" ] && cp "bin/Release/$ASM.pdb" "$OUT/" || true
cp "$ASM/ModInfo.xml" "$OUT/"
cp "$ASM/snapbounty.xml" "$OUT/"
cp "$ASM"/Config/*.xml "$OUT/Config/"

echo ">> done: $OUT"
ls -la "$OUT"

if [ -n "${DEPLOY_DIR:-}" ]; then
  rm -rf "$DEPLOY_DIR/$ASM"
  mkdir -p "$DEPLOY_DIR"
  cp -R "$OUT" "$DEPLOY_DIR/$ASM"
  echo ">> deployed to $DEPLOY_DIR/$ASM"
fi
