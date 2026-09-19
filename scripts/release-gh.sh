#!/usr/bin/env bash
# Builds the Thunderstore package (release.sh) and creates a GitHub release with
# the zip attached (tag v<version>). The release body is the CHANGELOG.md entry
# for this version (falling back to auto-generated notes when there is none).
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

# 2. Use the CHANGELOG.md entry for this version as the release body. The notes
#    file is only written when a "## <version>" section exists; python also
#    prints the previous release tag (for a compare link) to stdout.
notes="$repo_root/dist/RELEASE_NOTES-$version.md"
rm -f "$notes"
prev_tag="$(python3 - "$repo_root/CHANGELOG.md" "$version" "$notes" <<'PY' || true
import re, sys
changelog, version, out = sys.argv[1], sys.argv[2], sys.argv[3]
sections = re.split(r'(?m)^## ', open(changelog).read())
for i, section in enumerate(sections):
    title, _, rest = section.partition('\n')
    if title.strip() != version:
        continue
    body = rest.strip()
    if body:
        open(out, 'w').write(body + '\n')
        # The section right below this one is the previous release.
        if i + 1 < len(sections):
            prev, _, _ = sections[i + 1].partition('\n')
            prev = prev.strip()
            if re.fullmatch(r'v?\d+\.\d+\.\d+', prev):
                print(prev if prev.startswith('v') else 'v' + prev)
    break
PY
)"

if [[ -f "$notes" ]]; then
    if [[ -n "$prev_tag" ]]; then
        owner="$(gh repo view --json nameWithOwner -q .nameWithOwner)"
        printf '\n**Full Changelog**: [%s...%s](https://github.com/%s/compare/%s...%s)\n' \
            "$prev_tag" "$tag" "$owner" "$prev_tag" "$tag" >> "$notes"
    fi
    notes_args=(--notes-file "$notes")
else
    notes_args=(--generate-notes)
    echo "==> No CHANGELOG entry for $version; using auto-generated release notes"
fi

# 3. Publish (or update) the GitHub release with the zip attached.
if gh release view "$tag" >/dev/null 2>&1; then
    echo "==> GitHub release '$tag' already exists; updating body and uploading the zip"
    if [[ -f "$notes" ]]; then
        gh release edit "$tag" --notes-file "$notes"
    fi
    gh release upload "$tag" "$zip" --clobber
else
    echo "==> Creating GitHub release '$tag'"
    gh release create "$tag" "$zip" --title "Lucy was Replaced $version" "${notes_args[@]}"
fi

echo "==> Done: https://github.com/$(gh repo view --json nameWithOwner -q .nameWithOwner)/releases/tag/$tag"