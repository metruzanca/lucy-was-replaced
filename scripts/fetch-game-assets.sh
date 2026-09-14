#!/usr/bin/env bash
# Copies the game's managed assemblies into libs/ so the plugin can reference
# them, and downloads the pinned Gleam browser compiler.
#
# The game DLLs are proprietary; they must NEVER be committed to the repo.
#
# Usage:
#   ./scripts/fetch-game-assets.sh
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
libs_dir="$repo_root/libs"
embed_dir="$repo_root/src/Plugin/Embedded"
game_dir="${TFWR_GAME_DIR:-$HOME/.local/share/Steam/steamapps/common/The Farmer Was Replaced}"

GLEAM_VERSION="v1.18.1"
GLEAM_TARBALL="gleam-${GLEAM_VERSION}-browser.tar.gz"

managed_dir="$game_dir/TheFarmerWasReplaced_Data/Managed"
mkdir -p "$libs_dir" "$embed_dir"

if [[ ! -d "$managed_dir" ]]; then
  echo "error: game managed directory not found at: $managed_dir" >&2
  echo "Set TFWR_GAME_DIR to the game install (e.g. a Windows path)." >&2
  exit 1
fi

echo "==> Copying game assemblies"
for dll in Core Utils Assembly-CSharp NewAssembly mscorlib netstandard; do
  if [[ -f "$managed_dir/$dll.dll" ]]; then
    cp "$managed_dir/$dll.dll" "$libs_dir/"
    echo "    $dll.dll"
  else
    echo "    (skipping $dll.dll - not present)"
  fi
done
# UnityEngine refs (only the ones the plugin needs to compile against)
for dll in UnityEngine.CoreModule UnityEngine UnityEngine.UI UnityEngine.TextRenderingModule Unity.TextMeshPro UnityEngine.IMGUIModule UnityEngine.UIModule UnityEngine.InputLegacyModule UnityEngine.JSONSerializeModule; do
  if [[ -f "$managed_dir/$dll.dll" ]]; then
    cp "$managed_dir/$dll.dll" "$libs_dir/"
  fi
done

echo "==> Downloading Gleam browser compiler ${GLEAM_VERSION}"
curl -L "https://github.com/gleam-lang/gleam/releases/download/${GLEAM_VERSION}/${GLEAM_TARBALL}" -o "$embed_dir/$GLEAM_TARBALL"
tar xzf "$embed_dir/$GLEAM_TARBALL" -C "$embed_dir"
rm "$embed_dir/$GLEAM_TARBALL"
echo "    extracted to $embed_dir"

echo "==> Done."
echo
echo "NOTE: libs/ is gitignored - the game assemblies must not be committed."