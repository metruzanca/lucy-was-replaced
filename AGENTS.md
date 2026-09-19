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
- Package + GitHub release: `mise release-gh` (builds, then creates/updates the `v<version>` release; the release body is the `CHANGELOG.md` entry for that version)

## Commits (Conventional Commits)

Commit messages must follow [Conventional Commits](https://www.conventionalcommits.org/) so
git history stays readable and the auto-generated release notes are sensible (used only when
a version has no `CHANGELOG.md` entry yet).

Format: `<type>(<scope>): <subject>`

Types:
- `feat:` — new feature
- `fix:` — bug fix
- `perf:` — performance change
- `refactor:` — no behavior change
- `docs:` — docs only (README, `docs/`, AGENTS.md, CHANGELOG)
- `test:` — tests only
- `build:` — packaging/build (package.sh, mise.toml, scripts)
- `ci:` — CI only
- `chore:` — maintenance/tooling
- `style:` — formatting, no behavior change
- `revert:`
- Breaking change: append `!` (`feat!:`/`fix!:`) or add a `BREAKING CHANGE:` footer.

Rules:
- Subject: imperative mood, lowercase, no trailing period, ≤ 72 chars.
- Scope is optional, e.g. `fix(game):`, `feat(drones):`.
- One logical change per commit.
- Prefer the standard types over repo-historical prefixes: `feat` (not `feature`),
  `build` (not `pkg`/`release`), `chore` (not `tooling`), `docs` (not `plan`).

## Player-facing docs (README & CHANGELOG)

The GitHub README **is** the Thunderstore page — `scripts/package.sh` copies `README.md`
and `CHANGELOG.md` into every release bundle. Keep them in sync by editing only the repo
files; never edit the copy in a bundle. Write both for the player:

- **Non-technical and approachable.** Assume a beginner programmer who may never have
  heard of Gleam. No internal architecture, library names, or build details (no WASM,
  Jint, Harmony, "the bridge", "op-accounted", package scripts, …).
- **Changelog is user-experience-driven.** Entries describe what the player sees or feels
  in the game, never how it's implemented. Good: "Code windows stay in sync with a real
  Gleam project on disk, so your editor gets full autocomplete and error checking." Bad:
  "Added GleamProjectSync files→windows sync." Same rule applies to the README.

## In-game flow & debugging

- The plugin auto-stages into the game dir on build (`StagePlugin` target):
  `~/.local/share/Steam/steamapps/common/The Farmer Was Replaced/BepInEx/plugins/GleamFarmer`.
- BepInEx log: `<game>/BepInEx/LogOutput.log` (full exception stacks land here).
- The game hot-reloads `.py` files in the save dir into open code windows (file watcher,
  `Saver.cs`) — that's how `push-to-game-save.sh` works. Requires the "file watcher" setting,
  the window open, and an in-place write (`cp`); atomic rename-replace may not trigger.
- External-editor mode (`ExternalProject` config, default on): the mod mirrors windows to a
  real Gleam project at `<save>/gleam-project/` (real `gleam.toml`/`manifest.toml`, LSP stubs
  `src/game.gleam`/`game/item.gleam`/`game_ffi.mjs`), syncs via `GleamProjectSync`
  (`FileSystemWatcher` files→windows + flush windows→files on Run/save), and Run compiles the
  project from disk (`GleamRunner.CompileFromProject`). The game only watches top-level `.py`,
  so the subfolder is invisible to it. Debug with a real editor: open the project dir and run
  `gleam deps download` once, then `gleam check` gives the same errors the mod surfaces.
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