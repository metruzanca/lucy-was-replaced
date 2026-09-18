#!/usr/bin/env bash
# Builds the Thunderstore package (release.sh) and creates a GitHub release with
# the zip attached (tag v<version>).
#
# Usage:
#   ./scripts/release-gh.sh          # or: mise release-gh
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"

# 1. Build + verify the package.
"$repo_root/scripts/release.sh"

version="$(grep -oP '(?<=<Version>)[^<]+' "$repo_root/src/Plugin/GleamFarmer.csproj" | head -1)"
zip="$repo_root/dist/GleamFarmer-$version.zip"
tag="v$version"

# 2. Publish (or update) the GitHub release with the zip attached.
if gh release view "$tag" >/dev/null 2>&1; then
  echo "==> GitHub release '$tag' already exists; uploading the zip to it"
  gh release upload "$tag" "$zip" --clobber
else
  echo "==> Creating GitHub release '$tag'"
  gh release create "$tag" "$zip" --title "Lucy was Replaced $version" --generate-notes
fi

echo "==> Done: https://github.com/$(gh repo view --json nameWithOwner -q .nameWithOwner)/releases/tag/$tag"