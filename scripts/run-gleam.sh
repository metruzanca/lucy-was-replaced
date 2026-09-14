#!/usr/bin/env bash
# Compile + run a Gleam file headlessly (same runtime the plugin uses) for fast
# iteration without launching the game.
#
# Usage:
#   ./scripts/run-gleam.sh examples/hello.gleam
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
file="${1:?usage: run-gleam.sh <file.gleam>}"
file="$(cd "$(dirname "$file")" && pwd)/$(basename "$file")"

nix-shell "$repo_root/shell.nix" --run "dotnet run --project '$repo_root/tools/GleamHarness' -- --file '$file'"