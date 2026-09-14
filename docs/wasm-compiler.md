# Embedded Gleam WASM compiler — API reference

Source: `gleam-v1.18.1-browser.tar.gz` (official release asset, Apache-2.0).
Extracted into `src/Plugin/Embedded/`:
- `gleam_wasm_bg.wasm` (4.7 MB) — the compiler, wasm-bindgen ABI
- `gleam_wasm.js` — JS glue (reimplemented in C# as `WasmGlue`)
- `gleam_wasm.d.ts` — authoritative type surface

## Host imports the wasm requires (from `gleam_wasm.js`)

All strings are passed as `(ptr, len)` into linear memory. Decode UTF-8, then free
with the wasm export `__wbindgen_free(ptr, len, 1)`.

| Import | Signature | C# behaviour |
|---|---|---|
| `__wbg___wbindgen_throw_1506f2235d1bdba0` | `(ptr, len)` | Surface as a Gleam compiler panic |
| `__wbg_error_a6fa202b58aa1cd3` | `(ptr, len)` | Log error, then free |
| `__wbg_log_0c201ade58bb55e1` | 4×`(ptr, len)` | Log up to 4 strings, free each |
| `__wbg_log_ce2c4456b290c5e7` | `(ptr, len)` | Log 1 string, free |
| `__wbg_mark_b4d943f3bc2d2404` | `(ptr, len)` | No-op (performance.mark) |
| `__wbg_measure_84362959e621a2c1` | 2×`(ptr, len)` | No-op (performance.measure), free |
| `__wbg_new_227d7c05414eb861` | `() -> externref idx` | Create an Error object, store in externref mirror, return index |
| `__wbg_stack_3b0d974bbf31e44f` | `(out_ptr, externref idx)` | Write `mirror[idx].stack` as (ptr,len) at `out_ptr` (2×i32) |
| `__wbindgen_cast_0000000000000001` | `(ptr, len) -> externref idx` | `Ref(String) -> Externref`: store decoded string in mirror, return index |
| `__wbindgen_init_externref_table` | `()` | Grow exported `__wbindgen_externrefs` table by 4; set sentinels 0=undef, offset+0=undef, offset+1=null, offset+2=true, offset+3=false |

## Wasm exports (usable directly)

- `compile_package(project_id, target_ptr, target_len) -> (ret0, ret1)` — returns
  `(ptr, len)` normally; on error `ret1 != 0` and `ret0` is an externref index whose
  mirror value is the formatted compile-error **string** (wasm-bindgen
  `Result<(), String>` → `__wbindgen_cast`).
- `write_module(project_id, name_ptr, name_len, code_ptr, code_len)`
- `read_compiled_javascript(project_id, module_ptr, module_len) -> (ptr, len)` — `ptr==0` → module not compiled
- `read_compiled_erlang(...)`, `read_file_bytes(...)`, `write_file(...)`, `write_file_bytes(...)`
- `pop_warning(project_id) -> (ptr, len)` — `ptr==0` → no more warnings
- `reset_warnings(project_id)`, `reset_filesystem(project_id)`, `delete_project(project_id)`
- `initialise_panic_hook(debug)`
- `__wbindgen_start()` — must be called once after instantiation
- `__wbindgen_malloc(n, align)`, `__wbindgen_realloc(...)`, `__wbindgen_free(ptr, len, align)`
- `__externref_table_alloc()`, `__externref_table_dealloc(idx)`
- `__wbindgen_externrefs` (exported externref table), `__wbindgen_exn_store(idx)`
- `ring_core_0_17_13__bn_mul_mont(...)` — internal (checksum hashing), no host action

## project_id management

The playground's `static/compiler.js` wrapper: `newProject()` returns the next
incrementing id (0, 1, …). The wasm keeps its own project map internally; host code
just passes the id. Port that wrapper 1:1 into C# (`Compiler`/`Project` classes).

## Compile errors vs warnings

- **Errors** are thrown from `compile_package` (externref-cast string) — the Gleam
  diagnostics formatted for humans.
- **Warnings** are drained via repeated `pop_warning(project_id)` until `ptr==0`.

## Recommended C# glue

`src/GleamRuntime/WasmGlue.cs` + `GleamCompiler.cs` (Compiler/Project classes).
Bind the 10 imports above with Wasmtime's `Linker`, keep an `externref mirror`
(`List<object?>` index → value) since only host glue reads/writes table values.