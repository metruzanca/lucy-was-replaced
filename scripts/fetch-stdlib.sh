#!/usr/bin/env bash
# Fetches the pinned gleam_stdlib sources + externals and the JS runtime prelude
# into src/GleamRuntime/Embedded/. These are committed (Apache-2.0) so the plugin
# builds reproducibly without network access.
#
# Usage:
#   ./scripts/fetch-stdlib.sh
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
out_dir="$repo_root/src/GleamRuntime/Embedded/stdlib"
prelude_dir="$repo_root/src/GleamRuntime/Embedded"
tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

STDLIB_VERSION="1.0.5"

rm -rf "$out_dir"
mkdir -p "$out_dir"

echo "==> Downloading gleam_stdlib ${STDLIB_VERSION} from Hex"
curl -sL "https://repo.hex.pm/tarballs/gleam_stdlib-${STDLIB_VERSION}.tar" -o "$tmp_dir/stdlib.tar"
mkdir -p "$tmp_dir/stdlib"
tar xf "$tmp_dir/stdlib.tar" -C "$tmp_dir/stdlib"
tar xf "$tmp_dir/stdlib/contents.tar.gz" -C "$tmp_dir/stdlib"

echo "==> Copying gleam module sources (src/gleam -> stdlib/gleam)"
cp -r "$tmp_dir/stdlib/src/gleam" "$out_dir/gleam"

echo "==> Copying JS externals"
for f in "$tmp_dir/stdlib/src/"*.mjs; do
  cp "$f" "$out_dir/"
done

echo "==> Generating JS prelude (gleam export javascript-prelude)"
gleam export javascript-prelude > "$prelude_dir/prelude.mjs"

echo "==> Done."
find "$out_dir" -name "*.gleam" | wc -l