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

# 2. Use the CHANGELOG.md entry for this version as the release body and derive a
#    one-line title summary from its first bullet. Python writes the body to
#    "$notes" (only when a "## <version>" section exists) and prints the previous
#    release tag plus the summary, TAB-separated.
notes="$repo_root/dist/RELEASE_NOTES-$version.md"
rm -f "$notes"
prev_tag=""
summary=""
read -r prev_tag summary < <(
  python3 - "$repo_root/CHANGELOG.md" "$version" "$notes" <<'PY'
import re, sys
changelog, version, out = sys.argv[1], sys.argv[2], sys.argv[3]
sections = re.split(r'(?m)^## ', open(changelog).read())
prev, summary = "", ""
for i, section in enumerate(sections):
    title, _, rest = section.partition('\n')
    if title.strip() != version:
        continue
    body_lines = [l for l in rest.strip().splitlines() if l.strip()]
    if body_lines:
        open(out, 'w').write('\n'.join(body_lines) + '\n')
        first = body_lines[0].lstrip('- ').strip()
        for cut in (':', ',', '\u2014'):
            at = first.find(cut)
            if 0 < at <= 48:
                first = first[:at].rstrip()
                break
        if len(first) > 48:
            first = first[:48].rstrip()
            if ' ' in first:
                first = first[:first.rfind(' ')]
        summary = first
        # The section right below this one is the previous release.
        if i + 1 < len(sections):
            p, _, _ = sections[i + 1].partition('\n')
            p = p.strip()
            if re.fullmatch(r'v?\d+\.\d+\.\d+', p):
                prev = p if p.startswith('v') else 'v' + p
    break
print(f"{prev}\t{summary}")
PY
) || true

# Release title: version first, then the headline from the changelog, so the
# release list shows the version clearly (e.g. "v0.2.0 — external editing").
if [[ -n "$summary" ]]; then
    title="v$version — $summary"
else
    title="v$version"
fi

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
    echo "==> GitHub release '$tag' already exists; updating title, body, and zip"
    gh release edit "$tag" --title "$title"
    if [[ -f "$notes" ]]; then
        gh release edit "$tag" --notes-file "$notes"
    fi
    gh release upload "$tag" "$zip" --clobber
else
    echo "==> Creating GitHub release '$tag'"
    gh release create "$tag" "$zip" --title "$title" "${notes_args[@]}"
fi

echo "==> Done: https://github.com/$(gh repo view --json nameWithOwner -q .nameWithOwner)/releases/tag/$tag"