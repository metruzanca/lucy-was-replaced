#!/usr/bin/env bash
# Builds the plugin in Release and assembles a distributable BepInEx plugin zip.
# The zip extracts into the game root: BepInEx/plugins/GleamFarmer/...
#
# Usage:
#   ./scripts/package.sh
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(grep -oP '(?<=<Version>)[^<]+' "$repo_root/src/Plugin/GleamFarmer.csproj" | head -1)"
dist="$repo_root/dist"
pkg="$dist/GleamFarmer-$version"
plug="$pkg/BepInEx/plugins/GleamFarmer"

# Managed assemblies the plugin needs (game assemblies are NOT redistributed).
# The System.* netstandard facades are required: the game's Mono lacks them (verified:
# absent from TheFarmerWasReplaced_Data/Managed). System.ValueTuple is NOT needed — the
# game's mscorlib provides ValueTuple built-in.
dlls=(
  GleamFarmer GleamRuntime Jint Acornima Wasmtime.Dotnet IndexRange
  System.Memory System.Buffers System.Runtime.CompilerServices.Unsafe
  System.Numerics.Vectors
)

rm -rf "$pkg"
mkdir -p "$plug/Embedded"

echo "==> Writing manifest.json (Thunderstore)"
cat > "$pkg/manifest.json" <<JSON
{
  "name": "Lucy_Was_Replaced",
  "version_number": "$version",
  "website_url": "https://github.com/metruzanca/tfwr-gleam",
  "description": "Replace The Farmer Was Replaced's Python editor with the Gleam language.",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2305"
  ]
}
JSON

echo "==> Building plugin v$version (Release)"
dotnet build "$repo_root/src/Plugin/GleamFarmer.csproj" -c Release -p:GameDir=/nonexistent
bin="$repo_root/src/Plugin/bin/Release/net47"

echo "==> Copying managed assemblies"
for dll in "${dlls[@]}"; do
  if [[ ! -f "$bin/$dll.dll" ]]; then
    echo "error: missing $bin/$dll.dll" >&2
    exit 1
  fi
  cp "$bin/$dll.dll" "$plug/"
done

echo "==> Copying native wasmtime.dll (win-x64)"
native="$(find "$HOME/.nuget/packages/wasmtime" -path "*win-x64/native/wasmtime.dll" | sort | tail -1)"
[[ -n "$native" ]] || { echo "error: native wasmtime.dll not found" >&2; exit 1; }
cp "$native" "$plug/"

echo "==> Copying embedded assets (compiler wasm, stdlib, game module, prelude)"
cp -r "$repo_root/src/GleamRuntime/Embedded/." "$plug/Embedded/"
cp -r "$repo_root/src/Plugin/Embedded/." "$plug/Embedded/"
# Drop the fetch markers/glue docs not needed at runtime.
rm -f "$plug/Embedded/.gitignore"

echo "==> Copying docs"
# Thunderstore requires README.md / icon.png / manifest.json / CHANGELOG.md at the zip root.
cp "$repo_root/README.md" "$pkg/README.md"
cp "$repo_root/assets/icon.png" "$pkg/icon.png"
cp "$repo_root/CHANGELOG.md" "$pkg/CHANGELOG.md"
# Manual-install guide travels with the plugin (kept out of the game root).
cp "$repo_root/docs/INSTALL.md" "$plug/"
# The in-game Gleam reference, so manual-install players can read it next to the guide.
cp "$repo_root/src/GleamRuntime/Embedded/docs/game-reference.md" "$plug/game-reference.md"

echo "==> Zipping"
rm -f "$dist/GleamFarmer-$version.zip"
python3 - "$dist" "$pkg" "GleamFarmer-$version.zip" <<'PY'
import os, sys, zipfile
dist, pkg, out = sys.argv[1], sys.argv[2], sys.argv[3]
with zipfile.ZipFile(os.path.join(dist, out), "w", zipfile.ZIP_DEFLATED) as z:
    for root, _, files in os.walk(pkg):
        for f in files:
            full = os.path.join(root, f)
            # Entries relative to the package dir so manifest/README/icon/CHANGELOG sit at
            # the zip root (Thunderstore requirement) and BepInEx/ mirrors the game root.
            z.write(full, os.path.relpath(full, pkg))
print(f"wrote {out}")
PY

echo "==> Done: $dist/GleamFarmer-$version.zip"
du -h "$dist/GleamFarmer-$version.zip"
unzip -l "$dist/GleamFarmer-$version.zip" 2>/dev/null | tail -3 || true