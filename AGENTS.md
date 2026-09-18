# GleamFarmer repo conventions

## New `.gleam` files

When creating a new Gleam file (examples, scratch, snippets), start from the
repo template (`examples/template.gleam`):

```gleam
import game

pub fn main() {

}
```

## Common commands

- Build: `dotnet build GleamFarmer.sln` (inside `nix-shell`)
- Tests: `dotnet test tests/GleamRuntime.Tests`
- Run a Gleam file headless: `./scripts/run-gleam.sh examples/<name>.gleam`
- Hot-reload into the game: `./scripts/push-to-game-save.sh examples/<name>.gleam gleam`
- Build the Thunderstore package: `mise release` (project `mise.toml` task → `scripts/release.sh`)

## In-game flow & debugging

- The plugin auto-stages into the game dir on build (`StagePlugin` target):
  `~/.local/share/Steam/steamapps/common/The Farmer Was Replaced/BepInEx/plugins/GleamFarmer`.
- BepInEx log: `<game>/BepInEx/LogOutput.log` (full exception stacks land here).
- The game hot-reloads `.py` files in the save dir into open code windows (file watcher,
  `Saver.cs`) — that's how `push-to-game-save.sh` works. Requires the "file watcher" setting,
  the window open, and an in-place write (`cp`); atomic rename-replace may not trigger.
- An empty `pub fn main() {}` body compiles to a `todo` call — running it errors with
  "`todo` expression evaluated".

## Architecture gotchas

- Run = worker thread + Jint engine; bridge ops dispatch to the Unity main thread via
  `MainThreadDispatcher`, which surfaces failures as `AggregateException` (unwrap before
  showing errors).
- Game verbs are called with a bare `ProgramState` (`NewProgramState()`). Its
  `currentExecutingNode` is null, so the game's warning path (`Logger.LogWarning` →
  `ProgramState.GetTrace`) would NRE — fixed by `ProgramStateTracePatch`. Don't reintroduce
  call sites that log warnings with a bare state.
- `ResourceManager` lazy-loads at game start and can hand back half-populated `FarmObjectSO`s
  (null `cost`/`placeableOn`) — guard before `Plant`/`get_cost`.
- Pacing: use the `Stopwatch.GetTimestamp()`/`Frequency` pair (consistent); `Elapsed.Ticks`
  advances at a different rate than `Frequency` reports on this runtime. `OpWeights` AST cache
  is per-engine; the shared `TickPacer` is lock-serialized (one global op budget across drones).
- Multi-drone: one worker thread + Jint engine per drone; workers must be `pub fn` (resolved
  by compiled `.name`); headless drone tests use `CompiledGleam.Run(..., enableDrones: true)`.

## Gleam ↔ modules

- Every open code window is an importable Gleam module; the window you run is the entry.
  Cross-module helpers need `pub fn` (private fns are a compile error at the use site).
- Module names must match `^[a-z][a-z0-9_]*$`; names reserved by the mod: `game`, `gleam`,
  `gleam_stdlib`, `dict`, `game_ffi` (see `GleamModuleNames`).
- Gleam compiles self tail-recursion to `while(true)` loops; closures to block-bodied arrows;
  function names are preserved in JS output. Function param type annotations are optional
  (inferred).
- The game editor's indentation keys are problematic (shift-tab opens the Steam overlay) —
  M9 work item: auto-format on Run via a bundled `gleam` CLI.