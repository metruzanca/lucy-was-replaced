# GleamFarmer

Replace *The Farmer Was Replaced*'s built-in Python subset with **Gleam**.

Players write real Gleam in the game's editor (or an external `.gleam` file). A BepInEx
plugin compiles it with the actual Gleam compiler (embedded as WASM), runs the generated
JavaScript in an embedded engine, and bridges the farm API through a typed `game` module —
paced like the game's own interpreter (each action costs ops and takes real time).

## How it works

```
Gleam source (in-game editor)
  → Gleam compiler (WASM, via Wasmtime) → JavaScript
  → Jint (embedded JS engine) on a worker thread
  → game_ffi.mjs → C# bridge → the game's Drone/Farm/GridManager (main thread, paced)
```

- Actions (`game.move`, `game.harvest`, `game.plant`, `game.till`, `io.println`) block the
  worker for `ops × OpDuration` real seconds — the drone animates and the sim clock keeps
  running, so speed upgrades scale pacing.
- Sensors (`get_pos_x`, `get_entity_type`, `num_items`, …) read state and return instantly.
- Run/Execute toggles: press to start, press again to stop.

## Modules

```gleam
import game          // move, harvest, plant, till, get_* sensors, custom types
import game/item     // item.num_items, item.use_item
```

```gleam
game.till()
game.plant(game.Carrot)
game.move(game.North)
game.item.num_items(game.item.Hay)
```

## Development

Environment (NixOS-friendly dev shell):

```sh
nix-shell            # dotnet-sdk, gleam, node
./scripts/fetch-game-assets.sh    # copies game DLLs → libs/ + downloads pinned wasm compiler
./scripts/fetch-stdlib.sh         # gleam_stdlib + prelude → src/GleamRuntime/Embedded
dotnet build GleamFarmer.sln
dotnet test tests/GleamRuntime.Tests
```

Iteration:

```sh
./scripts/run-gleam.sh examples/hello.gleam       # compile+run headless (no game)
./scripts/push-to-game-save.sh examples/verify.gleam gleam   # hot-reload into the game
./scripts/copy-save-progression.sh Save0 Gleam    # grant a save another save's unlocks/items
```

## In-game setup (Linux/Proton)

1. Install BepInEx 5 (win_x64) into the game directory.
2. Apply the Wine DLL override so the game loads BepInEx's winhttp proxy
   (`./scripts/apply-proton-override.sh`, run once with the game closed).
3. `dotnet build src/Plugin/GleamFarmer.csproj` stages the plugin into
   `BepInEx/plugins/GleamFarmer/` (DLLs + embedded wasm/stdlib + native `wasmtime.dll`).
4. Launch the game, open a save, press Run.

Output and errors land in `BepInEx/LogOutput.log`; `io.println` also renders as the game's
floating print bubbles above the drone.

## Layout

- `src/GleamRuntime/` — compiler WASM host (Wasmtime + wasm-bindgen glue), stdlib/prelude
  bundling, Jint ESM host, `game` FFI bridge interfaces.
- `src/Plugin/` — BepInEx plugin: Harmony patches (Run intercept, Python-parse skip, Gleam
  syntax highlighting), paced worker execution, real game bridge.
- `examples/` — runnable Gleam scripts.
- `docs/` — research notes (game internals, wasm compiler ABI, runtime decision).

## Status

- M0–M3: interpreter pipeline replaced; plugin runs Gleam in-game; highlighting; external
  `.gleam`/`.py` hot-reload; headless CLI.
- M4: paced `game` FFI — movement + planting verified in-game; sensors built on the same
  mapping. See `gleamfarmer.plan.md` for the checklist.