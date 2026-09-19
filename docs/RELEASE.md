# Releasing GleamFarmer

How to produce and ship a new `dist/GleamFarmer-<version>.zip`.

## Prerequisites

Everything is pinned, so first make sure the pinned assets are present and
up-to-date (re-run if they were ever deleted or the pins changed):

```sh
nix-shell                                   # .NET SDK + gleam toolchain
./scripts/fetch-game-assets.sh              # copies game DLLs → libs/ (gitignored),
                                            # downloads gleam-v1.18.1-browser.tar.gz
./scripts/fetch-stdlib.sh                   # gleam_stdlib 1.0.5 + prelude → src/GleamRuntime/Embedded
```

The game must be closed if you later run a Debug build (the `StagePlugin` target
copies into the live game's `BepInEx/plugins/GleamFarmer`).

## Steps

### 1. Bump the version

`src/Plugin/GleamFarmer.csproj`:

```xml
<Version>0.1.0</Version>
```

Semver. The zip name is derived from this value.

### 2. Build + package

```sh
./scripts/package.sh
```

This:

1. Builds the plugin in **Release** (`-p:GameDir=/nonexistent`, so it does *not*
   touch the live game install).
2. Assembles a clean plugin folder from `bin/Release/net47/`:
   - managed assemblies: `GleamFarmer`, `GleamRuntime`, `Jint`, `Acornima`,
     `Wasmtime.Dotnet`, `IndexRange`, `System.Memory`, `System.Buffers`,
     `System.Runtime.CompilerServices.Unsafe`, `System.Numerics.Vectors`
   - native `wasmtime.dll` (win-x64) from the NuGet cache
   - `Embedded/` (compiler wasm, stdlib, `game` modules, prelude, `game_ffi.mjs`)
   - `INSTALL.md`, `THIRD_PARTY_NOTICES.md` inside the plugin folder

   The four `System.*` facades are required — the game's Mono does **not** ship them
   (verified absent from `TheFarmerWasReplaced_Data/Managed/`); `Acornima`/`Wasmtime`
   depend on `System.Memory`/`System.Runtime.CompilerServices.Unsafe`. `System.ValueTuple`
   is *not* bundled: the game's `mscorlib` provides ValueTuple built-in.
3. Adds the Thunderstore metadata at the zip root: `manifest.json` (generated from the
   csproj version), `README.md`, `icon.png` (from `assets/icon.png`), `CHANGELOG.md`.
4. Zips it into `dist/GleamFarmer-<version>.zip` (entries at the zip root, so manual
   install = extract into the game root).

The zip extracts into the **game root**: `BepInEx/plugins/GleamFarmer/...`.

### 3. Verify the zip

```sh
unzip -l dist/GleamFarmer-0.1.0.zip        # inspect layout
python3 -c "import zipfile; z=zipfile.ZipFile('dist/GleamFarmer-0.1.0.zip'); print(z.testzip())"  # integrity
```

Checks:
- Exactly 11 `.dll`s (see the list above) — **no game assemblies** (`Core.dll`,
  `Utils.dll`, `UnityEngine*`, `Unity.TextMeshPro`) and no .NET Standard facade
  shims beyond the four required ones (`System.AppContext.dll`,
  `System.Collections*.dll`, … are not bundled — the game provides those).
- `Embedded/gleam_wasm_bg.wasm`, `Embedded/stdlib/`, `Embedded/game.gleam`,
  `Embedded/game/`, `Embedded/prelude.mjs`, `Embedded/game_ffi.mjs` present.

> **Why the four `System.*` facades are bundled:** `System.Memory`,
> `System.Buffers`, `System.Runtime.CompilerServices.Unsafe` and
> `System.Numerics.Vectors` are real dependencies (Acornima + Wasmtime) and the
> game's Mono ships **none** of them in `TheFarmerWasReplaced_Data/Managed/` —
> removing them breaks loading. Thunderstore's "some .dll files may be
> unnecessary" warning is a false positive here; `System.ValueTuple` *was*
> redundant (the game's mscorlib provides it) and is not bundled.

### 4. Validate against a clean install

The most reliable check is the minimal DLL set in the live game:

```sh
P="<game>/BepInEx/plugins/GleamFarmer"
rm -rf "$P"
cp -r dist/GleamFarmer-<version>/BepInEx/plugins/GleamFarmer "$P"
```

Launch the game, press Run, and confirm `BepInEx/LogOutput.log` shows
`GleamFarmer runtime ready (19 stdlib modules)` and the script output. (The
minimal set has been validated once; re-check after dependency changes.)

## Notes / gotchas

- **Version pins:** compiler `v1.18.1` (asset tarball) and `gleam_stdlib 1.0.5`
  are locked. If you bump either, update `scripts/fetch-*.sh` *and* the wasm
  ABI glue (`docs/wasm-compiler.md`) and re-run the full test suite.
- **Wasmtime:** the native `wasmtime.dll` (win-x64) is fetched from the NuGet
  cache at package time; the managed binding must stay version-locked with it
  (`Wasmtime` 44.0.0 in `src/GleamRuntime/GleamRuntime.csproj`).
- **Dev vs release installs:** `dotnet build src/Plugin/GleamFarmer.csproj`
  (Debug) stages the same files into the live game for iterating; the zip is
  the distributable artifact. Keep the two in sync by re-staging after changes.
- **Test before shipping:** `dotnet test tests/GleamRuntime.Tests` (headless)
  plus the `examples/verify.gleam` in-game run (movement + farming + sensors).

## Publish to Thunderstore

The package zip is already Thunderstore-compatible (`manifest.json`, `README.md`,
`icon.png`, `CHANGELOG.md` at the zip root, plus `BepInEx/plugins/GleamFarmer/…`).

### Manually

1. **Build** the zip: `mise release` (wraps `./scripts/release.sh`, which builds
   `./scripts/package.sh` and verifies the result).
2. **Validate**:
   - `unzip -l dist/GleamFarmer-<version>.zip` — root must list `manifest.json`,
     `README.md`, `icon.png`, `CHANGELOG.md`, then `BepInEx/plugins/GleamFarmer/…`.
   - Run the manifest through Thunderstore's
     [Manifest Validator](https://thunderstore.io/tools/manifest-v1-validator/).
3. **Upload** (web):
   - Sign in at thunderstore.io (GitHub login works).
   - *Create package* → choose the **The Farmer Was Replaced** community → your team.
   - Upload the zip; the manifest fills in name/version/description/dependencies.
4. **Dependency**: `BepInEx-BepInExPack-5.4.2305` (auto-installed by the mod manager).
5. **Versioning**: bump `<Version>` in `GleamFarmer.csproj` and re-upload per release
   (semver; Thunderstore shows the highest version regardless of upload order).

## Checklist

- [ ] Assets fetched (`fetch-game-assets.sh`, `fetch-stdlib.sh`)
- [ ] Version bumped in `GleamFarmer.csproj`
- [ ] `mise release`
- [ ] Zip inspected (11 DLLs, Embedded present, no game assemblies)
- [ ] Headless tests pass
- [ ] Clean-install in-game smoke test passes
- [ ] Zip uploaded / shared with `docs/INSTALL.md`