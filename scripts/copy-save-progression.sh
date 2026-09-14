#!/usr/bin/env bash
# Copies `items` + `unlocks` from a donor save into a target save's save.json,
# preserving the target's window layout (positions, docked files, docs).
# Handy for giving a fresh testing save enough progression to explore the API.
#
# Usage:
#   ./scripts/copy-save-progression.sh [fromSave] [toSave]   # defaults: Save0 -> Gleam
set -euo pipefail

base="${TFWR_SAVES_DIR:-$HOME/.local/share/Steam/steamapps/compatdata/2060160/pfx/drive_c/users/steamuser/AppData/LocalLow/TheFarmerWasReplaced/TheFarmerWasReplaced/Saves}"
from="${1:-Save0}"
to="${2:-Gleam}"

from_dir="$(find "$base" -maxdepth 1 -type d -iname "$from" | head -1)"
to_dir="$(find "$base" -maxdepth 1 -type d -iname "$to" | head -1)"
[[ -z "$from_dir" || -z "$to_dir" ]] && { echo "error: save dirs not found" >&2; exit 1; }

from_json="$from_dir/save.json"
to_json="$to_dir/save.json"
[[ -f "$from_json" && -f "$to_json" ]] || { echo "error: save.json missing" >&2; exit 1; }

cp "$to_json" "$to_json.bak"
python3 - "$from_json" "$to_json" <<'PY'
import json, sys

with open(sys.argv[1]) as f:
    donor = json.load(f)
with open(sys.argv[2]) as f:
    target = json.load(f)

target["items"] = donor["items"]
target["unlocks"] = donor["unlocks"]

with open(sys.argv[2], "w") as f:
    json.dump(target, f)

print(f"donor unlocks: {len(donor['unlocks'])}  items: {len(donor['items']['serializeList'])}")
print(f"target kept window layout; backup at {sys.argv[2]}.bak")
PY