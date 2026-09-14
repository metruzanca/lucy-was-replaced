# WASM runtime decision

**Decision: Wasmtime .NET (`Wasmtime` 44.0.0), driven by a hand-written C# port of the
wasm-bindgen JS glue.**

## What the browser build actually is

`gleam-v1.18.1-browser.tar.gz` is a **wasm-bindgen** build (`wasm-pack build --target web`),
not emscripten:

- `gleam_wasm_bg.wasm` — the compiler (4.7 MB)
- `gleam_wasm.js` — wasm-bindgen glue (10 host imports + string marshalling)
- `gleam_wasm.d.ts` — the exported API surface

This is much easier to host than emscripten: no `env` memory/table plumbing; only ten
small JS imports and an exported `externref` table.

## ABI notes (learned the hard way)

The build uses the **modern wasm-bindgen externref ABI**:

- `__wbg_new_227d7c05414eb861` is `() -> externref` (returns an actual externref value).
- `__wbindgen_cast_0000000000000001` is `(i32, i32) -> externref`.
- `__wbg_stack_3b0d974bbf31e44f` is `(i32, externref) -> ()`.
- `compile_package` still returns `(i32, i32)`; on failure `ret1 != 0` and `ret0` is an
  **index into the exported `__wbindgen_externrefs` table** holding the error value.
- `__wbindgen_init_externref_table` (a host import) grows the exported table by 4 and
  seeds sentinels: `0 = undefined`, `offset+0 = undefined`, `offset+1 = null`,
  `offset+2 = true`, `offset+3 = false`.
- Input strings to `write_module` / `compile_package` / `read_*` are owned by Rust
  (do **not** free). Returned strings from `read_*` / `pop_warning` are host-owned
  (free with `__wbindgen_free(ptr, len, 1)`).

Wasmtime .NET provides everything needed: `Config.WithReferenceTypes(true)`,
`object` ↔ externref marshalling, `Caller.GetFunction` (call exports from imports),
`Table.GetElement`/`SetElement`/`Grow`, and `Memory.ReadString`/`WriteString`/`ReadInt32`.

## Spike result

`tools/GleamHarness` (net8.0 console) loads the wasm through `GleamRuntime.GleamWasm` and
successfully:

1. compiles `pub fn main() { Nil }` → `export function main() { return undefined; }`
2. compiles a two-module package and reads both modules' ESM
3. surfaces a full Gleam syntax diagnostic (`src/main.gleam:1:23`) as a thrown exception
4. drains warnings via `pop_warning`

No fallback runtime was needed.
