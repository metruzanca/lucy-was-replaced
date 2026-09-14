#!/usr/bin/env bash
# Pushes a Gleam file into the running game's save directory. The game's file
# watcher hot-reloads it into the code window, so you can iterate: edit a
# snippet, run this, press Run in the game.
#
# Usage:
#   ./scripts/push-to-game-save.sh examples/hello.gleam [saveName]
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
file="${1:?usage: push-to-game-save.sh <file.gleam> [saveName]}"
save="${2:-gleam}"

save_dir="${TFWR_SAVE_DIR:-}"
if [[ -z "$save_dir" ]]; then
  base="$HOME/.local/share/Steam/steamapps/compatdata/2060160/pfx/drive_c/users/steamuser/AppData/LocalLow/TheFarmerWasReplaced/TheFarmerWasReplaced/Saves"
  # Save dirs are matched case-insensitively (the game capitalizes window/save names).
  save_dir="$(find "$base" -maxdepth 1 -type d -iname "$save" | head -1)"
fi

if [[ ! -d "$save_dir" ]]; then
  echo "error: save dir not found: $save_dir" >&2
  exit 1
fi

echo "==> Pushing $(basename "$file") -> $save_dir/main.py"
cp "$file" "$save_dir/main.py"
echo "    (the game should hot-reload this into the 'main' window)"