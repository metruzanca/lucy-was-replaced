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

### M1 — WASM compiler spike (de-risk first)
- [ ] Download + pin `gleam-v1.18.1-browser.tar.gz` into `src/Plugin/Embedded/`
- [ ] Headless console harness: load `gleam.wasm` via Wasmtime, implement emscripten import shim
- [ ] Exercise `newProject` / `writeModule` / `compilePackage("javascript")` / `readCompiledJavaScript` / `takeWarnings`
- [ ] Compile + run `io.println("Hello")` end-to-end in the harness
- [ ] Fallback check: same spike under pure-managed `dotnet-webassembly` (if Wasmtime fails)
- [ ] Record runtime decision in `docs/wasm-runtime.md`

### M2 — GleamRuntime library (headless)
- [ ] WASM compiler wrapper (`GleamCompiler.cs`) with pinned compiler version
- [ ] Bundle + write `gleam_stdlib` sources (pin matching version, e.g. 1.0.5) into the project
- [ ] Read all compiled ESM modules into an in-memory module map
- [ ] Jint host (`JsRuntime.cs`): ESM module loader, `io`/`console` capture, execution timeout
- [ ] Gleam error mapping (`src/main.gleam:N` → editor line)
- [ ] `tfwr` FFI stub module (typed `@external` functions; initially log-only)
- [ ] xunit suite: recursion, `case`, stdlib `list`/`string`, error cases
- [ ] Confirm full-language support (records, `use`, imports) compiles + runs

### M3 — In-game plugin
- [ ] Harmony patch: intercept run entry → route `CodeWindow.codeText` to GleamRuntime
- [ ] Route output/errors to game output area + `Plugin.Log`; wire stop
- [ ] Gleam editor syntax coloring via `CodeUtilities.colors` patch
- [ ] FarmerLib mod options: toggle Gleam mode, view compiled JS
- [ ] Install BepInEx into game dir; test under Proton
- [ ] Acceptance: a Gleam program typed in-game runs and prints to the game console

### M4 — Farm API FFI (explicitly later)
- [ ] Implement game verbs in JS host backed by publicized `Core.dll` (Farm/GridManager/inventory)
- [ ] Value marshalling across Jint↔CLR
- [ ] Smoke test: automate a real farm task from Gleam

### M5 — Packaging & teaching docs
- [ ] BepInEx plugin zip (DLL + embedded wasm/stdlib + Wasmtime/Jint deps)
- [ ] README + install instructions
- [ ] Functional-paradigm sample scripts (pure fns, state threading, `map`/`fold`, pipelines, `case`)

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

## M1 details — emscripten shim risk

The browser build is emscripten output; its `.wasm` imports `env` functions (memory/table,
`fd_write`, etc.). M1 must implement these in the chosen runtime. This is the single
biggest unknown and is isolated first.

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
