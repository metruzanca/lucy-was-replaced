#!/usr/bin/env bash
# Forces the game's Wine prefix to load the app-dir winhttp.dll (the BepInEx
# doorstop proxy) instead of Wine's builtin. Run with the game CLOSED.
#
# Usage:
#   ./scripts/apply-proton-override.sh
set -euo pipefail

GAME_DIR="${TFWR_GAME_DIR:-$HOME/.local/share/Steam/steamapps/common/The Farmer Was Replaced}"
COMPAT_DATA="${TFWR_COMPAT_DATA:-$HOME/.local/share/Steam/steamapps/compatdata/2060160}"
PROTON_ROOT="${TFWR_PROTON_ROOT:-$HOME/.local/share/Steam/steamapps/common/Proton - Experimental}"

if [[ ! -f "$COMPAT_DATA/pfx/user.reg" ]]; then
  echo "error: no prefix found at $COMPAT_DATA/pfx" >&2
  exit 1
fi

echo "==> Setting winhttp=native,builtin for TheFarmerWasReplaced.exe"
WINEPREFIX="$COMPAT_DATA/pfx" "$PROTON_ROOT/files/bin/wine" reg add \
  "HKCU\\Software\\Wine\\AppDefaults\\TheFarmerWasReplaced.exe\\DllOverrides" \
  /v winhttp /d "native,builtin" /f

echo "==> Verifying"
WINEPREFIX="$COMPAT_DATA/pfx" "$PROTON_ROOT/files/bin/wine" reg query \
  "HKCU\\Software\\Wine\\AppDefaults\\TheFarmerWasReplaced.exe\\DllOverrides" /v winhttp

echo "==> Done. Relaunch the game via Steam."