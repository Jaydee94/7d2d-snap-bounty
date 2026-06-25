#!/usr/bin/env bash
# Stamps the version computed by semantic-release into SnapBounty/ModInfo.xml and packs
# the mod into dist/SnapBounty-v<version>.zip (containing the SnapBounty/ folder).
# Expects the DLL to already be built at bin/Release/.
#
# Usage: scripts/stage-release.sh <version>
set -euo pipefail

VERSION="${1:?version missing}"
ASM="SnapBounty"

# 7DTD ModInfo expects a 4-part version -> pad if needed.
FOUR="$VERSION"
case "$VERSION" in
  *.*.*.*) ;;
  *.*.*)   FOUR="$VERSION.0" ;;
  *.*)     FOUR="$VERSION.0.0" ;;
  *)       FOUR="$VERSION.0.0.0" ;;
esac

sed -i.bak -E "s#(<Version value=\")[^\"]*(\")#\1${FOUR}\2#" "$ASM/ModInfo.xml" && rm -f "$ASM/ModInfo.xml.bak"

STAGING="staging/$ASM"
rm -rf staging dist
mkdir -p "$STAGING/Config" dist

cp "bin/Release/$ASM.dll" "$STAGING/"
[ -f "bin/Release/$ASM.pdb" ] && cp "bin/Release/$ASM.pdb" "$STAGING/" || true
cp "$ASM/ModInfo.xml" "$STAGING/"
cp "$ASM"/Config/*.xml "$STAGING/Config/"
cp README.md "$STAGING/" 2>/dev/null || true

( cd staging && zip -r "../dist/${ASM}-v${VERSION}.zip" "$ASM" >/dev/null )
echo "staged dist/${ASM}-v${VERSION}.zip (ModInfo version ${FOUR})"
