# Development

Technical reference for *Lucy was Replaced* (the repo). For players, see the
[README](../README.md) and [INSTALL.md](INSTALL.md).

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
- **Multiple modules**: every open code window is an importable Gleam module — create a
  `utils` window and `import utils` from another window (helpers you call across modules need
  `pub fn`). The window you run is the entry point.
- **External editing (Gleam project)**: with the `ExternalProject` config enabled (default),
  the mod mirrors the windows to a real Gleam project at `<save>/gleam-project/` (a subfolder
  of the save dir — the game only watches top-level `.py` files, so it's invisible to the
  game). `GleamProjectSync` scaffolds `gleam.toml`/`manifest.toml` (pinned to the embedded
  gleam_stdlib) plus `src/game.gleam`/`game/item.gleam`/`game_ffi.mjs` as LSP stubs, watches
  `src/*.gleam` via `FileSystemWatcher` to push external edits into open windows, flushes
  windows → files on Run and on `Saver.SaveCode`, and seeds windows from the project on load.
  Run compiles the project from disk (`GleamRunner.CompileFromProject`), so the LSP and the
  runtime read the same bytes. This makes full Gleam LSP work in an external editor.
  `SaverSaveCodePatch`/`SaverLoadPatch` are the Harmony hooks.
- **Full tick model**: pure Gleam computation is op-accounted too. Jint's debugger fires per
  executed statement; each statement's AST is weighted against the game's tick rules
  (binary op = 1, if branch = 1, loop start = 1, index = 1; calls/reads free), then all ops
  (computation + actions) are paced to the tick rate and fed to `get_tick_count`. Power
  drains like the game's interpreter (`UsedPower += ops/200/30`).
- Sensors (`get_pos`, `get_entity_type`, `num_items`, …) read state and return instantly.
- `get_time()` returns op-accounted execution time (`ops × OpDuration`); the world clock is
  unchanged. Run/Execute toggles: press to start, press again to stop.

## Game module reference

The quick reference below. A full, per-function reference is also available **in-game**
(Gleam mode): open the docs window — the builtins list shows `game.*`, and hovering any
`game.X` / `game.item.Y` in the editor shows its Gleam doc. The bundled source of truth
for that panel is `src/GleamRuntime/Embedded/docs/game-reference.md` (one `## ` section
per function/constant; it renders on GitHub).

```gleam
import game            // movement, farming, sensors, utilities, custom types
import game/item       // item.num_items, item.use_item, item.use_items
```

```gleam
game.till()
game.plant(game.Carrot)
game.move(game.North)
item.num_items(item.Hay)
let pos = game.get_pos()          // game.Position(x, y)
game.swap(game.East)              // move the tile's entity to the adjacent tile
game.clear()                      // wipe the farm
game.measure()                    // petal count / cactus size / treasure position
game.get_cost(game.Carrot)        // seed cost as items
game.unlock(game.Carrot)          // spend resources to unlock an entity
game.unlock_by_name("multi_trade")
game.set_world_size(4)
game.do_a_flip()
```

Multi-drone:

```gleam
import game

pub fn main() {
  let handles = game.spawn_drone(3, worker)   // worker must be a pub fn
  case handles {
    [a, b, c, ..] -> {
      let _ = game.wait_for(a)
      let _ = game.wait_for(b)
      let _ = game.wait_for(c)
    }
    _ -> echo "no drones"
  }
}

pub fn worker() -> Nil {
  let me = game.get_drone_id()
  // ... farm this drone's quadrant ...
  Nil
}
```

Drones run concurrently, each in its own engine/thread, sharing one tick budget and the
world (actions serialize on the main thread). `game.send(message, drone_id)` /
`game.receive()` coordinate via mailboxes; `game.has_finished(handle)` polls;
`game.spawn_drone_with` lets a worker return a value for `game.wait_for`.

Custom types: `game.Direction`, `game.Entity`, `game.Ground`, `game.Position`,
`game.Companion` (from `get_companion`), `game.item.Item`, `game.DroneHandle`.

Full `game` surface — actions (paced): `move`, `can_move`, `harvest`, `can_harvest`,
`plant`, `till`, `swap`, `clear`, `use_item`, `unlock`, `unlock_item`,
`set_execution_speed`, `set_world_size`, `do_a_flip`, `pet_the_piggy`, `change_hat`,
`quick_print` (free). Logging: `io.println` (paced, like `print`) and `echo value`
(free, like `quick_print`). Sensors (instant): `get_pos`, `get_world_size`,
`get_entity_type`, `get_ground_type`, `get_water`, `measure`, `measure_at`,
`get_companion`, `get_cost`, `num_items`, `num_unlocked`, `num_unlocked_item`,
`num_drones`, `max_drones`, `random`, `get_time`, `get_tick_count`. Drones:
`spawn_drone`, `spawn_drone_with`, `get_drone_id`, `wait_for`, `has_finished`, `send`,
`receive`, `receive_from`.

## Environment

NixOS-friendly dev shell:

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

Release: `mise release` builds the Thunderstore package; `mise release-gh` also creates the
GitHub release. See [RELEASE.md](RELEASE.md).

Output and errors land in `BepInEx/LogOutput.log`; `io.println` also renders as the game's
floating print bubbles above the drone.

## Repository layout

- `src/GleamRuntime/` — compiler WASM host (Wasmtime + wasm-bindgen glue), stdlib/prelude
  bundling, Jint ESM host, `game` FFI bridge interfaces, on-disk project source
  (`GleamProjectSource`, `CompileFromProject`).
- `src/Plugin/` — BepInEx plugin: Harmony patches (Run intercept, Python-parse skip, Gleam
  syntax highlighting), paced worker execution, real game bridge, external-editor project
  sync (`GleamProjectSync` + `SaverSaveCodePatch`/`SaverLoadPatch`).
- `src/Plugin/Embedded/project-template/` — the `gleam.toml`/`manifest.toml` scaffold
  shipped for external-editor projects.
- `examples/` — runnable Gleam scripts.
- `docs/` — research notes (game internals, wasm compiler ABI, runtime decision).

## Status

- M0–M3: interpreter pipeline replaced; plugin runs Gleam in-game; highlighting; external
  `.gleam`/`.py` hot-reload; headless CLI.
- M4: paced `game` FFI — movement + planting verified in-game; sensors built on the same
  mapping. M4-3 adds the full utility builtin set (swap, clear, measure, costs, unlock,
  drones, cosmetics) with 9/9 headless tests. See `gleamfarmer.plan.md` for the checklist.