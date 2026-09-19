# Game internals recon (current Steam build, appid 2060160)

Assemblies (Steam install, `TheFarmerWasReplaced_Data/Managed/`, copied to `libs/`):
- `Core.dll` (~348 KB) — interpreter core (`ProgLang/`), UI (`Core/UI/`), farm/content
- `Utils.dll` (~273 KB) — PyTypes, `CodeUtilities`, menu options, LeanTween, etc.
- `Assembly-CSharp.dll` (~12 KB), `NewAssembly.dll` (~8 KB) — thin bootstrap stubs

Source paths are embedded in the DLLs (PBD-style), so the original layout is known:
`Assets/Scripts/Core/ProgLang/*` and `Assets/Scripts/Core/UI/*`.

## Interpreter pipeline (`Assets/Scripts/Core/ProgLang/`)
- `Tokenizer.cs`, `TokenStream.cs`, `Parser.cs`, `Scope.cs`, `ProgramState.cs`,
  `ModuleState.cs`, `Execution.cs`, `BuiltinFunctions.cs`, `Logger.cs`
- AST nodes: `Nodes/{Node, SequenceNode, AssignmentNode, BinaryExprNode, BracketNode,
  BranchNode, BreakNode, CallNode, ComparisonNode, ContinueNode, DefNode, DictNode,
  ForNode, FunctionNode, ImportNode, ListNode, LiteralNode, NoOpNode, PassNode,
  ReturnNode, SetNode, TupleNode, UnaryExprNode, ValueNode}.cs`
- `Node.Execute` is generator-based (returns `IEnumerable<double>`, tick accounting).

## Run / stop entry points (method-name candidates from strings)
- `Execution.StartProgramExecution`, `StartMainExecution`, `StartStepByStepMode`,
  `StopProgramExecution`, `RunNextStep`, `StartFileWatcher` / `StopFileWatcher`
  (external-editor hot reload)
- `CodeWindow.codeText` — the editor buffer holding the source
- `CodeUtilities` (in `Utils.dll`) — syntax highlighting via a `colors` regex list
  (patchable, as `enhanced-python` does)

> NOTE: this current build renamed the interpreter class (`Interpreter` → `Execution`).
> The `tfwr-modding/enhanced-python` mod (2024) targets an older build. The signatures
> below were pinned against the decompiled `Core.dll` (see `docs/decompiled/`).

## PyTypes (`Assets/Scripts/Utils/PyTypes/`, in `Utils.dll`)
`PyObject` base + `PyNumber, PyString, PyBool, PyList, PyTuple, PyDict, PySet, PyRange,
PyNone, PyUnassigned, PyConstBag, PyModule, PyDroneHandle, PyGridDirection,
InternalPySequence`. No `PyClass` in the vanilla build (classes were a mod addition).

## Language subset (documented behaviour — the FFI / teaching target)
Community references:
- Wiki: https://thefarmerwasreplaced.wiki.gg/wiki/
- Reference interpreter (pure Python model of the subset):
  https://github.com/LucasCerattoRS/the-farmer-was-replaced-lab

## Integration strategy
Do NOT drive the game's Python pipeline. Instead:
1. Harmony-patch the run entry so pressing Run/Execute compiles `CodeWindow.codeText`
   (Gleam) with the embedded WASM compiler → JavaScript.
2. Execute that JS in Jint, bridging game verbs through a `game` FFI module backed by
   publicized `Core.dll` farm/grid/inventory state.
3. Patch `CodeUtilities.colors` for Gleam-aware syntax highlighting.
4. Surface output/errors in the game's console / `Plugin.Log`.

All four are implemented in `src/Plugin/Patches/` + `RealGameBridge.cs`.