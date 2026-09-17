#!/usr/bin/env bash
# Local Thunderstore release: builds the package (package.sh), verifies the zip
# layout and manifest, then prints the upload steps.
#
# Usage:
#   ./scripts/release.sh
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"

# 1. Build the package.
"$repo_root/scripts/package.sh"

# 2. Verify the zip layout + manifest.
version="$(grep -oP '(?<=<Version>)[^<]+' "$repo_root/src/Plugin/GleamFarmer.csproj" | head -1)"
zip="$repo_root/dist/GleamFarmer-$version.zip"

python3 - "$zip" <<'PY'
import json, sys, zipfile
z = zipfile.ZipFile(sys.argv[1])
names = set(z.namelist())

for required in ("manifest.json", "README.md", "icon.png", "CHANGELOG.md"):
    if required not in names:
        raise SystemExit(f"error: missing {required} at the zip root")
if not any(n.startswith("BepInEx/plugins/GleamFarmer/") for n in names):
    raise SystemExit("error: missing BepInEx/plugins/GleamFarmer/")
banned = [n for n in names if n.endswith(("Core.dll", "Utils.dll", "Unity.TextMeshPro.dll"))]
if banned:
    raise SystemExit(f"error: game assemblies must not ship: {banned}")
if not any("gleam_wasm_bg.wasm" in n for n in names):
    raise SystemExit("error: missing Embedded/gleam_wasm_bg.wasm")

manifest = json.loads(z.read("manifest.json"))
print(f"==> Verified {manifest['name']} {manifest['version_number']}")
print(f"    dependency: {', '.join(manifest['dependencies'])}")
print(f"    entries: {len(names)}")
PY

echo "==> Done: $zip"
echo
echo "Upload to Thunderstore:"
echo "  1. Sign in at thunderstore.io (GitHub login) -> Create package"
echo "  2. Community: The Farmer Was Replaced -> your team"
echo "  3. Upload: $zip"
echo "  (BepInEx-BepInExPack-5.4.2305 is declared in the manifest and auto-installs)"