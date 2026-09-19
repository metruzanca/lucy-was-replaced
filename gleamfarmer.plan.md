# GleamFarmer — Replace Python with Gleam in *The Farmer Was Replaced*

Status: **Planning complete, implementation not started**
Owner: metru
Last updated: 2026-09-14

A BepInEx plugin that lets players write **Gleam** in TFWR's in-game editor instead of
the built-in Python subset, so the game can be used to teach a functional paradigm.

---

## Checklist

Legend: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked

### M0 — Environment & recon
- [x] Add Nix devshell (`shell.nix`): `dotnet-sdk`, `gleam`, `nodejs` (ilspycmd via dotnet tool)
- [x] `scripts/fetch-game-assets.sh`: copy `Core.dll`, `Utils.dll`, UnityEngine refs from game install into `libs/`
- [x] Download + pin `gleam-v1.18.1-browser.tar.gz` into `src/Plugin/Embedded/`
- [x] Decode the WASM compiler ABI (`docs/wasm-compiler.md`): wasm-bindgen imports/exports, project_id, error/warning model
- [x] Write `docs/game-internals.md`: interpreter layout, run/stop entry candidates, PyTypes, integration strategy
- [ ] Decompile current `Core.dll` (`ProgLang/Execution.cs`, `Parser.cs`, `Tokenizer.cs`, `CodeWindow.cs`, `BuiltinFunctions.cs`) with ilspycmd
- [ ] Initialize `GleamFarmer.sln` + `GleamRuntime.csproj` + `Plugin.csproj` (net47, publicize Core/Utils)

### M1 — WASM compiler spike (de-risk first)  ✅ COMPLETE
- [x] Download + pin `gleam-v1.18.1-browser.tar.gz` into `src/Plugin/Embedded/`
- [x] Headless console harness: load `gleam_wasm_bg.wasm` via Wasmtime, implement the wasm-bindgen host imports (the build is wasm-bindgen, not emscripten)
- [x] Exercise `write_module` / `compile_package("javascript")` / `read_compiled_javascript` / `pop_warning`
- [x] Compile + read `hello` and a two-module package end-to-end in the harness
- [x] Fallback check: not needed — Wasmtime handles the externref ABI
- [x] Record runtime decision in `docs/wasm-runtime.md`

### M2 — GleamRuntime library (headless)  ✅ COMPLETE
- [x] WASM compiler wrapper (`GleamWasm.cs`, `GleamCompiler.cs`) with pinned compiler version
- [x] Bundle + write `gleam_stdlib` 1.0.5 sources + JS externals + prelude into the project (`Embedded/`, fetched by `scripts/fetch-stdlib.sh`)
- [x] Read all compiled ESM modules into an in-memory module map
- [x] Jint host (`JsRuntime.cs`): ESM module loader (custom `IModuleLoader` resolving `./x.mjs`/`../x.mjs`), `io`/`console` capture, execution timeout + recursion limit
- [x] Gleam error mapping: compile errors carry `src/main.gleam:N:C` diagnostics
- [x] `tfwr` FFI stub module (typed `@external` functions + `tfwr_ffi.mjs` stub routing to console)
- [x] xunit suite (7 tests, all green): hello, stdlib, records, `use`, tfwr FFI, compile-error location, timeout
- [x] Confirm full-language support: records/record-update, `use`, closures, pipelines all compile + run

### M3 — In-game plugin ✅ CORE DONE
- [x] Decompile current `Core.dll`/`Utils.dll` with ilspycmd → `docs/decompiled/` (gitignored)
- [x] Create `src/Plugin` BepInEx net47 project (publicize Core/Utils, reference game DLLs from `libs/`)
- [x] Harmony patch: `CodeWindow.PressExecuteOrStop` prefix → route `CodeInput.text` to GleamRuntime
- [x] Harmony patch: `CodeWindow.Parse` skipped in Gleam mode (fixes bogus Python "invalid file import" errors)
- [x] Route output/errors to `Plugin.Log`; compile errors surfaced via `CodeWindow.SetErrorMessage`
- [x] Install BepInEx into game dir; Wine winhttp override (scripts/apply-proton-override.sh); tested under Proton ✅
- [x] Gleam editor syntax coloring: `CodeUtilities.SyntaxColor2` prefix → `GleamHighlighter` (keywords/functions/types/numbers/strings/comments)
- [ ] FarmerLib mod options: toggle Gleam mode (config entry exists; UI option later)
- [x] Acceptance: Gleam program typed in-game runs and prints ✅ (examples/hello.gleam → hello/total/strings in game log)

### M4 — `game` FFI (paced farm API) ✅ COMPLETE (v1: movement + farming + sensors; v2: utilities breadth)
Decisions: paced worker-thread execution (blocking FFI, ~ops*OpDuration waits); custom Gleam types for constants; v1 scope = movement + farming + sensors; v2 adds the full utility builtin set.
- [x] Rename `tfwr` → `game`: `game.gleam` (custom types Direction/Entity/Ground + pattern-match→code) + `game/item.gleam` (Item), `game_ffi.mjs` primitive host bridge, `StubGameBridge`, tests green (7/7)
- [x] `IGameBridge` + `StubGameBridge` wired into `JsRuntime` as `__gleam_host` (default stub; plugin passes real bridge later) — methods named to match JS (Jint is case-insensitive but no underscore stripping)
- [x] `RealGameBridge`: main-thread dispatch (plugin `Update()` pump), `sim.farm.drones[0]` calls, op-cost pacing waits, `ResourceManager` SO lookups
- [x] `PacedGleamRun`: worker thread + Jint engine, Run/Stop toggle (Run button stops active run), `StartExecutionMode`/`StopExecutionMode`, Jint CancellationToken for stop
- [x] `io.println` pacing via print handler (PrintToAir + ~1 s)
- [x] In-game verification ✅: movement paced, planting works, sensors correct (`ground: soil`, `entity: Carrot OK`, `carrots: 3104334863`); fixed int32 overflow + ResourceManager lazy-load race
- [x] Examples (`hello`, `farm`, `walk`, `verify`) + README + docs

Fast-iteration tooling:
- `scripts/run-gleam.sh <file.gleam>` — headless compile+run (same runtime as the plugin)
- `scripts/push-to-game-save.sh <file.gleam> [save]` — hot-reload a snippet into the game's editor via the file watcher

### M4 — Farm API FFI ✅ COMPLETE (v2: utilities breadth)
- [x] Implement game verbs in JS host backed by publicized `Core.dll` (Farm/GridManager/inventory)
- [x] Value marshalling across Jint↔CLR (primitives, null→Option, CLR arrays→JS lists via `Array.from` + `gleam/dynamic/decode`)
- [x] Cover all remaining game builtins as typed `game` functions: `swap`, `clear`, `measure`, `measure_at`, `get_companion`, `get_cost`, `random`, `num_drones`, `max_drones`, `unlock`, `num_unlocked`, `set_execution_speed`, `set_world_size`, `do_a_flip`, `pet_the_piggy`, `change_hat`, `quick_print`, plus `item.use_items`/`item.unlock_item`/`item.num_unlocked_item`
- [x] `use_item` now handles water **and** fertilizer, with inventory checks + count overload
- [x] Tests (9/9 green): all new verbs route to the bridge; typed decoding verified (measure `Some`, companion `None`, cost item pairs)
- [ ] Smoke test in-game: automate a real farm task using utilities (swap/measure/cost)

### M4-4 — API polish (idiomatic Gleam)
- [x] `game.Position` record + `get_pos()`; removed loose `get_pos_x`/`get_pos_y` from the public API
- [x] `game.Companion(entity, position)` record; `get_companion() -> Option(Companion)` (no nested tuples)
- [x] `item.use_item_n` → `item.use_items`
- [x] Teaching examples: `case`-checked actions + `use <- bool.guard` for sensor-gated actions
- [x] Qualified `decode.*`/`game/item` imports internally; unlocks keep the typed Entity/Item + `_by_name` escape hatch

### M4 — Deferred (explicitly later)
- ~~Multi-drone~~ ✅ (see M8)
- Meta: `leaderboard_run` / `simulate` / `tap`
- Pure-Python builtins (`range`/`len`/`min`/`max`/`abs`/`str`/`list`/`set`/`dict`) — use Gleam stdlib equivalents instead

### M5 — Packaging & distribution ✅ COMPLETE
- [x] `scripts/package.sh` → `dist/GleamFarmer-<version>.zip` (12 DLLs incl. native wasmtime, embedded wasm/stdlib/game, docs; no game assemblies)
- [x] `docs/INSTALL.md` (Windows + Linux/Proton incl. winhttp override) + `THIRD_PARTY_NOTICES.md`
- [x] Validated: clean packaged install (minimal DLL set) loads and runs in-game ✅
- [ ] Teaching content (functional-paradigm samples + guide) — separate future item

---

### M6 — Full tick-engine parity (op-accounted timing) ✅ COMPLETE
Decisions: count *all* ops (pure computation + actions) via Jint's debugger `Step` event,
weight each executed statement against the game's tick rules by walking its Acornima AST
once (cached per node), pace to `ops × OpDuration` like the interpreter's ≤199-op steps.
- [x] `TickEngine` (`OpAccumulator` + `OpWeights` AST cache + `TickPacer`): computation ticks merge with action ops into one counter
- [x] `OpWeights` calibration (probed Jint 4.16.2 step granularity): loop tests step separately, if-conditions fold into the IfStatement, per-call steps report `null` (free), Gleam closures compile to block bodies (stepped normally)
- [x] `JsRuntime` enables `Debugger.Enabled` + `StepMode.Into` when a `TickEngine` is supplied; `CompiledGleam.Run` threads it through
- [x] `RealGameBridge` routes `WaitOps` through the pacer, `get_tick_count` = merged ops, `get_time` = op-accounted (`ops × OpDuration`), power drain flushed to main thread (`UsedPower += ops/200/30`, mirrors Execution.cs:192)
- [x] `StubGameBridge` accounts standard action costs when wired to an accumulator (headless merge tests)
- [x] Tests (7): straight-line arithmetic, TCO loop iteration cost, non-tail recursion, action-op merge, `get_tick_count` value, real-time pacing, disabled-pacing speed
- [x] Fixed a `Stopwatch` landmine: `Elapsed.Ticks` advances at 100 ns (10 MHz) while `Frequency` reports 1 GHz on this runtime — the pacer now uses the consistent `GetTimestamp()`/`Frequency` pair
- [ ] In-game validation: verify a compute-heavy Gleam run paces at the selected execution speed and drains power

Known divergences (documented, deliberate):
- World/crop time tracks wall clock (the sim loop overwrites `sim.CurrentTime` when idle);
  `get_time()` is op-accounted from the run's own ops. Op-accounting world time would need a
  Harmony patch on `Simulation.RunNextStep`'s idle branch — deferred.
- Jint `LimitRecursion(4096)` still rejects very deep non-tail recursion (the game has no limit).

### M7 — Official leaderboards disabled ✅ COMPLETE
The game author requires all mods to disable the official (Steam) leaderboards, so modded
executions can't pollute them. `SteamLeaderboard.LoadLeaderboard` (Utils.dll) is the single
choke point every leaderboard interaction flows through (score upload + entry download), so
it's no-op'd with a Harmony prefix (`src/Plugin/Patches/LeaderboardDisablePatch.cs`). The
in-game leaderboard screen still opens; it just shows no data and submits nothing.
- [x] Harmony prefix on `SteamLeaderboard.LoadLeaderboard` → `__result = null`, skip original
- [x] Plugin builds clean (0 warnings/errors)
- [ ] In-game validation: leaderboard run finishes locally, nothing reaches Steam

### M7 — Custom Gleam leaderboard server (future)
Pledged in the plan (not implemented): if we want ranked play, host our own leaderboard
server (Gleam!) and route scores there instead of Steam. The `LeaderboardDisablePatch` keeps
official boards off; a future FFI (`game.leaderboard_*`) + a small Gleam HTTP server would
give the same challenge runs with a Gleam-native leaderboard. Untracked for now; the M4
deferred `leaderboard_run`/`simulate`/`tap` builtins could feed it.

---

### M8 — Multi-drone ✅ COMPLETE
Decisions: true concurrency (one worker thread + one Jint engine per drone) since Jint
cannot preempt a synchronous call stack; workers resolved **by name** — Gleam preserves
function names in its JS output, so a `pub fn` passed by value carries `.name`, and a fresh
drone engine imports it from the shared module graph (no closure reconstruction).
- [x] `DroneController` (GleamRuntime): child threads/engines, synthetic `__drone_<id>`
      modules (`import { worker } from "./main.mjs"; JSON.stringify(worker())`), completion
      signals, JSON mailboxes, `__gleam_drones` per-engine host, Stop/join
- [x] `IGameBridge` + `RealGameBridge` drone-id-aware (`add_drone`/`remove_drone`/`drone_generation`,
      `drones[_droneId]`); `StubGameBridge` for headless tests
- [x] `TickPacer` made thread-safe (shared global budget across engines); `OpWeights` cache
      per-engine (AST nodes are not shared across engines)
- [x] `game.gleam`/`game_ffi.mjs` drone API: `spawn_drone`/`spawn_drone_with`, `get_drone_id`,
      `wait_for`, `has_finished`, `send`, `receive`, `receive_from`, `DroneHandle`
      (null→`None` via `dynamic.classify`, results marshal as JSON `Dynamic`)
- [x] `PacedGleamRun` wires the controller; drone failure cancels the whole run
- [x] Tests (7): worker result via wait_for, 3 concurrent workers by id, send/receive,
      has_finished polling, anonymous-worker error, missing-handle None, infinite-worker stop
- [x] Example `examples/multidrone.gleam`
- [ ] In-game validation: 3-drone quadrant farm, send/receive coordination, stop with drones alive

Documented v1 limitations:
- Workers must be `pub fn` in the editor module (anonymous closures rejected with a clear error).
- Module-level state is per-engine (each drone re-evaluates the module), not shared like the game.
- Results/messages are JSON-only (`Dynamic`; decode with `gleam/dynamic/decode`).
- `send` delivers immediately, not op-delayed like the game's `MessageChannel`.
- `clear()` while drones run is best-effort (drone objects may be removed).

### M9 — Auto-format Gleam on Run (deferred work item)
Motivation: the game editor's indentation keys are painful (shift-tab is hijacked by the Steam
overlay), so formatting should just happen automatically. When the player presses Run:
**format → write formatted text back to the editor → compile the *same* formatted text**, so
diagnostics always point at the visible lines. Since we compile exactly what we show, the
formatter does not need to be byte-identical to `gleam format`.

Chosen approach (**Option B**): bundle the real `gleam` CLI and shell out, for exact `gleam format`
output. Research findings that shaped this:
- The embedded Gleam **WASM compiler exposes no `format` export** (formatter is in the binary,
  but the wasm-bindgen surface is compile-only — `gleam_wasm.d.ts` has no format).
- `gleam format` takes **file paths only** (`gleam format -` on stdin fails with "File IO
  failure"), so the plugin must write a temp file, run the CLI on it, read it back, and delete it.
- `gleam format` **refuses malformed code** (returns a syntax error), so on format failure the
  flow must fall back to compiling the original editor text untouched (brackets stay as typed,
  errors reference the original lines).

Implementation notes (Option B):
- Package a **win-x64 `gleam.exe`** with the mod (works natively on Windows and under Wine/Proton).
  Add a fetch step (like `fetch-stdlib.sh`) downloading the pinned `gleam-<ver>-x86_64-pc-windows-gnu.exe`
  release into `src/Plugin/Embedded/`; include it in `scripts/package.sh`'s DLL set.
- `GleamHost.Run(window)`: after `GetCodeText`, when a new `AutoFormat` config (default true) is
  set, spawn `gleam.exe format <tmp.gleam>` (temp file in the plugin dir), read back, `SetCodeText`
  (new reflection helper mirroring `GetCodeText`), then compile the formatted text.
- Errors: compile diagnostics are already `src/main.gleam:N:C`; with the editor showing the
  formatted text, N:C now points at the visible lines.
- Config toggle `AutoFormat` in `Config.Bind("General", "AutoFormat", true, …)` for players who
  want their exact formatting preserved.
- Tests: headless `GleamFormatterTests` are not applicable (real CLI), but add a `scripts/`
  helper + harness check that formats a fixture and compiles the result; verify idempotence
  (`gleam format` output is stable).
- Risks: process spawn per Run (~100-500 ms), temp-file hygiene (cleanup on stop/crash), Wine
  compatibility of spawning the Windows exe, +~25 MB package size.
- Alternative considered: custom C# reindenter (zero-dep, instant, no reflow, not byte-identical)
  — revisit if the CLI turns out too heavy to ship.
- [ ] fetch + package the win-x64 `gleam.exe`
- [ ] `AutoFormat` config + format→SetCodeText→compile flow in `GleamHost.Run`
- [ ] temp-file lifecycle + failure fallback (compile original)
- [ ] in-game validation: formatting fixes indentation, editor updates, errors align to formatted lines

---

### M10 — In-game Gleam reference (docs panel, tooltips, autocomplete) ✅ COMPLETE
The game's docs/info surfaces describe Python builtins (`plant(Entities.Carrot)`); in Gleam
mode they now show the `game.*` library (`game.plant(game.Carrot)`).
- Single source of truth: `src/GleamRuntime/Embedded/docs/game-reference.md` (one `## `
  section per `game` function/constant + overview with Python→stdlib mapping). Split on
  headings by `GleamRuntime.GleamDocs` into the game's doc page ids (`functions/*`,
  `objects/*`, `items/*`, `game`).
- Patches (Gleam-mode gated) swap the Python surfaces for the bundled reference:
  `Localizer.Localize` postfix (`code_tooltip_*` pages + hover-tooltip text, overview via
  `code_tooltip_game`, "Builtins" heading → "Game API"); `MarkdownText` TOC generators
  (builtins/entities/grounds/items list `game.*`, same unlock gates + leading module
  overview entry); `TooltipUtils.GetWordTooltip` prefix (`game.X` / `game.item.Y` → Gleam
  tooltip with clickable docs link) + `FarmObjectTooltip`/`ItemTooltip` postfixes
  (`objects/*`/`items/*` pages); `CodeWindow.GetWordList` (offer `game`, drop bare Python
  builtins) + `GetSubWordList` (`game.` / `game.item.` autocomplete).
- Out of scope: `__builtins__.py` save stub (external Python editors), `docs/scripting/*`
  (Python syntax guides), docs search box, non-English languages.
- [x] Content authored + loaded at startup (`GleamHost.Init`)
- [x] Docs-window TOC + per-function/entity/item pages show `game.*`
- [x] Hover tooltips for `game.X` / `game.item.Y`
- [x] `game.` / `game.item.` autocomplete
- [x] Tests (9): every `pub fn` in `game.gleam`/`item.gleam` has a page, TOC links resolve,
      signatures present, overview maps Python builtins, dotted lookups + autocomplete
- [ ] In-game validation: docs window, hover, autocomplete all read as `game.*`

### M10.5 — Gleam stdlib + primer docs in-game ✅ COMPLETE
Extends the M10 surfaces to the stdlib and to beginner guides.
- `GleamStdlibDocs` (GleamRuntime) extracts the curated modules (`bool`, `dict`, `float`,
  `function`, `int`, `list`, `option`, `order`, `pair`, `result`, `set`, `string`) from the
  embedded `.gleam` sources at runtime — module overview (leading `////` doc) + per-function
  pages (`///` doc + signature). Undocumented `pub fn`s are omitted; nested `## ` demoted to
  `### ` so the section split stays intact; bodyless `@external` signatures handled.
- `docs/gleam-primer.md`: ~6 beginner sections adapted from tour.gleam.run (Expressions,
  Functions, Case expressions, Records, Pipelines/`use`, Option/Result) — Apache-2.0.
- `GleamDocs`: `AddSection` maps primer/stdlib titles to `functions/gleam_*` page ids;
  `LookupDotted` resolves bare `int.absolute_value` / `list.map`; `Builtins()` stays
  game-only; `PrimerToc()` / `StdlibToc()` render a separate **"Gleam stdlib"** home-page
  section (numbered primer list first, then the module pages) via a `MarkdownText.Setup`
  prefix; `StdlibMembers()` drives autocomplete.
- Patches: `CodeWindowDocsPatch` offers the 12 module names + `int.`/`list.`/… subword
  domains (user-window shadowing respected); `LocalizerDocsPatch` serves
  `code_tooltip_gleam_*`/`_gleam_primer_*` (page ids `functions/gleam_*` flow through the
  existing `FunctionDoc` path unchanged).
- Tests (12): curated modules have overviews + autocomplete domains, documented functions
  get pages with signatures, `### Examples` demotion, dotted lookups, TOC resolves, primer
  sections loaded. Doc test classes now share the `gleam` collection (static `GleamDocs`
  state must not be mutated in parallel).
- [ ] In-game validation: `int.` autocomplete + hover tooltips + primer pages in the docs window

### M11 — External-editor Gleam project ✅ CORE DONE
Real on-disk Gleam project so players can edit code windows in an external editor with
full Gleam LSP (completion, hover, go-to-def, diagnostics, `gleam format`).
- `GleamProjectSource` (GleamRuntime): discover/load `src/*.gleam` (valid, non-reserved
  names; nested `game/*` and the `game`/`game_ffi` stubs excluded).
- `GleamRunner.CompileFromProject(projectDir, entryName, entrySource)` — compile the
  on-disk project (entry source overridable by the active window), so LSP and runtime
  read the same bytes.
- `GleamProjectSync` (Plugin): scaffolds `<save>/gleam-project/` (`gleam.toml` +
  `manifest.toml` pinning the embedded gleam_stdlib, copied `game.gleam`/`game/item.gleam`/
  `game_ffi.mjs` LSP stubs), `FileSystemWatcher` files→windows, `FlushWindows`
  windows→files on Run + `Saver.SaveCode`, `SeedWindowsFromProject` on `Saver.Load`.
- Config toggle `ExternalProject` (default true); disabled → old in-memory window compile.
- Bundled templates: `src/Plugin/Embedded/project-template/{gleam.toml,manifest.toml}`.
- Tests (5): module discovery filters, entry from disk, entry-source override, missing-entry
  error, disk-vs-memory parity. `gleam check` verified against the bundled template + stubs.
- [ ] In-game validation: scaffold appears on Run; external edit hot-reloads; save flushes;
      load seeds; `gleam check`/LSP in VS Code resolve `game.*` and stdlib.

---

## Decisions (locked)

1. **Architecture B** — run the *real* Gleam compiler via WASM (playground approach), not a hand-written Gleam parser. Full Gleam from day one.
2. **Pin Gleam v1.18.1** and matching `gleam_stdlib` (matches local `gleam 1.18.1`). Syntax is stable; revisit on demand.
3. **Ship Wasmtime** (native win-x64) for faster compilation/execution. Pure-managed runtime only as fallback.
4. **FFI (M4):** reimplement game verbs in C# against publicized `Core.dll` (recommended path).
5. **Name:** `GleamFarmer`. Repo dir stays `tfwr-mod-gleam`.
6. **Code entry:** in-game editor (transpile-on-run style interception), not external files.

---

## Architecture

```
CodeWindow.codeText (Gleam source)
  → Harmony patch intercepts game "Run"
  → GleamRuntime.CompileAndRun(source):
       Wasmtime → gleam.wasm
          project.writeModule("main", source)           // player code
          project.writeModule("<gleam_stdlib/*>", …)    // bundled stdlib sources
          project.writeModule("tfwr", …)                // FFI stub module (typed externals)
          project.compilePackage("javascript")
          readCompiledJavaScript(main + stdlib modules) → module map
       Jint engine:
          module loader serves the compiled ESM modules
          io/console shims → game output / plugin log
          host FFI functions (CLR-bound) → game (M4)
          execution constraints (timeout, recursion) → no infinite-loop hangs
          execute main(); map errors ("src/main.gleam:N") to the editor
```

### Why this works (verified)
- Official release asset exists and is small: `gleam-v1.18.1-browser.tar.gz` (~1.6 MB).
- The playground proves the WASM API: `newProject()`, `project.writeModule(name, code)`,
  `project.compilePackage("javascript")`, `project.readCompiledJavaScript("main")`,
  `project.takeWarnings()`.
- **Wasmtime** NuGet targets netstandard2.0 → runs on .NET Framework 4.6.1+ (game is
  net46/47), ships native `wasmtime.dll` (win-x64, loads under Proton).
- **Jint** targets netstandard2.0 + net462, supports ES2015–2020 (classes, destructuring,
  spread, modules, BigInt) and exposes CLR functions to JS — the FFI bridge.
- Game run path is Harmony-patchable (proven by `tfwr-modding/enhanced-python`).
- The current build renamed `Interpreter` → `Execution`; decompile the *current* DLLs
  rather than trusting the 2024 mod's class names.

---

## Repository layout

```
flake.nix / shell.nix          # dotnet-sdk, ilspycmd, local gleam
libs/                          # game's Core.dll, Utils.dll, UnityEngine.* (gitignored)
src/GleamRuntime/              # WASM-compiler wrapper + Jint host + stdlib bundling (pure C#)
src/Plugin/                    # BepInEx net47 plugin: Harmony patches, UI wiring, options
src/Plugin/Embedded/           # gleam.wasm + glue, gleam_stdlib sources, tfwr stub module
tests/                         # xunit: compile+run pure Gleam programs, assert output/errors
scripts/fetch-game-assets.sh   # copy game DLLs + download pinned compiler/stdlib
docs/                          # game-internals.md, wasm-runtime.md
gleamfarmer.plan.md            # this file
```

---

## M0 details — recon targets

Discovered so far from `strings` on the installed build (Steam appid 2060160):
- Game source paths embedded: `Assets/Scripts/Core/ProgLang/*` (`Execution.cs`,
  `Parser.cs`, `Tokenizer.cs`, `TokenStream.cs`, `Scope.cs`, `ProgramState.cs`,
  `ModuleState.cs`, `BuiltinFunctions.cs`) and `Assets/Scripts/Core/ProgLang/Nodes/*`.
- Run/stop candidates: `Execution.StartProgramExecution`, `StartMainExecution`,
  `StartStepByStepMode`, `StopProgramExecution`, `RunNextStep`, `StartFileWatcher`.
- Editor: `CodeWindow.codeText`; highlighting: `CodeUtilities.colors` (in `Utils.dll`).
- PyTypes (Utils): `PyObject`, `PyNumber`, `PyString`, `PyBool`, `PyList`, `PyTuple`,
  `PyDict`, `PySet`, `PyRange`, `PyNone`, `PyModule`, `PyDroneHandle`, `PyGridDirection`.

Decompile step must confirm: which method reads `codeText` and starts execution, and the
signatures of the game builtins for the M4 FFI.

## M1 details — host ABI risk (resolved)

The browser build is **wasm-bindgen** (not emscripten), so there is no emscripten
`env` shim. The risk was instead the externref ABI: `__wbg_new`/`__wbindgen_cast`
return real `externref` values, while `compile_package` errors come back as indexes
into the exported `__wbindgen_externrefs` table. M1 reimplemented the glue in C#
(`GleamWasm.cs`) and confirmed the whole pipeline under Wasmtime — see
`docs/wasm-runtime.md`.

## Risks & mitigations
- **Emscripten WASM under .NET** — isolated in M1; managed-runtime fallback.
- **Gleam JS output under Jint** — cross-check compiled output in Node locally; add polyfills.
- **Mono/.NET Framework loading** — target `net47`, test early under Proton.
- **Game version drift** — pin the decompiled build; M0 produces the patch map.
- **Infinite loops** — Jint execution timeouts; `panic`/unhandled `case` → game error UI.
- **Plan B** — transpile Gleam→game-Python (hand-written parser) if B stalls; simpler subset.

## Open questions
- None blocking. Revisit Wasmtime-vs-managed after M1.

---

## References
- Game modding org: https://github.com/tfwr-modding
  - Template: https://github.com/tfwr-modding/template
  - Complex example (language extension via Harmony): https://github.com/tfwr-modding/enhanced-python
  - Lib (options menu): https://github.com/tfwr-modding/lib
- BepInEx: https://github.com/BepInEx/BepInEx
- Gleam playground (WASM compiler pattern): https://github.com/gleam-lang/playground
- Gleam compiler browser release:
  `https://github.com/gleam-lang/gleam/releases/download/v1.18.1/gleam-v1.18.1-browser.tar.gz`
- Wasmtime .NET: https://docs.wasmtime.dev/lang-dotnet.html
- Jint: https://github.com/sebastienros/jint
- Managed WASM fallback: https://github.com/RyanLamansky/dotnet-webassembly
- TFWR language reference (community): https://thefarmerwasreplaced.wiki.gg/wiki/
- Reference interpreter (pure Python model): https://github.com/LucasCerattoRS/the-farmer-was-replaced-lab
