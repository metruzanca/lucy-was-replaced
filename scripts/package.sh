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
dlls=(
  GleamFarmer GleamRuntime Jint Acornima Wasmtime.Dotnet IndexRange
  System.Memory System.Buffers System.Runtime.CompilerServices.Unsafe
  System.Numerics.Vectors System.ValueTuple
)

rm -rf "$pkg"
mkdir -p "$plug/Embedded"

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
cp "$repo_root/THIRD_PARTY_NOTICES.md" "$pkg/"
cp "$repo_root/docs/INSTALL.md" "$pkg/"
cp "$repo_root/README.md" "$pkg/README.md"

echo "==> Zipping"
rm -f "$dist/GleamFarmer-$version.zip"
python3 - "$dist" "$pkg" "GleamFarmer-$version.zip" <<'PY'
import os, sys, zipfile
dist, pkg, out = sys.argv[1], sys.argv[2], sys.argv[3]
with zipfile.ZipFile(os.path.join(dist, out), "w", zipfile.ZIP_DEFLATED) as z:
    for root, _, files in os.walk(pkg):
        for f in files:
            full = os.path.join(root, f)
            z.write(full, os.path.relpath(full, dist))
print(f"wrote {out}")
PY

echo "==> Done: $dist/GleamFarmer-$version.zip"
du -h "$dist/GleamFarmer-$version.zip"
unzip -l "$dist/GleamFarmer-$version.zip" 2>/dev/null | tail -3 || true