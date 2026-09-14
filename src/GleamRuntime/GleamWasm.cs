using System;
using System.Text;
using Wasmtime;

namespace GleamRuntime
{
    /// <summary>Thrown when the Gleam compiler reports a compile error.</summary>
    public sealed class GleamCompileException : Exception
    {
        public GleamCompileException(string diagnostics) : base(diagnostics) { }
    }

    /// <summary>Thrown when the compiler wasm panics internally.</summary>
    public sealed class GleamPanicException : Exception
    {
        public GleamPanicException(string message) : base(message) { }
    }

    /// <summary>Receives console output emitted by the compiler wasm.</summary>
    public interface IGleamLogSink
    {
        void Log(string message);
        void Error(string message);
    }

    /// <summary>
    /// Low-level bridge to the Gleam compiler WASM module (wasm-bindgen ABI).
    ///
    /// This reimplements the JS glue (`gleam_wasm.js`) in C#: the ten host imports
    /// the module requires, the externref table, and UTF-8 string marshalling via
    /// the module's own `__wbindgen_malloc` / `__wbindgen_free`.
    /// </summary>
    public sealed class GleamWasm : IDisposable
    {
        private const string ImportModule = "./gleam_wasm_bg.js";

        private readonly Engine _engine;
        private readonly Store _store;
        private readonly Module _module;
        private readonly Linker _linker;
        private readonly Instance _instance;

        private readonly Memory _memory;
        private readonly Table _externrefTable;

        private readonly Function _compilePackage;
        private readonly Function _writeModule;
        private readonly Function _writeFile;
        private readonly Function _writeFileBytes;
        private readonly Function _readJs;
        private readonly Function _readErl;
        private readonly Function _readFileBytes;
        private readonly Function _popWarning;
        private readonly Function _resetWarnings;
        private readonly Function _resetFilesystem;
        private readonly Function _deleteProject;
        private readonly Function _malloc;
        private readonly Function _free;
        private readonly Function _externrefDealloc;

        public IGleamLogSink? LogSink { get; set; }

        public GleamWasm(byte[] wasmBytes)
        {
            var config = new Config().WithReferenceTypes(true);
            _engine = new Engine(config);
            _store = new Store(_engine);
            _module = Module.FromBytes(_engine, "gleam_wasm_bg.wasm", wasmBytes);

            var linker = new Linker(_engine);
            DefineImports(linker);
            _linker = linker;

            _instance = linker.Instantiate(_store, _module);

            _memory = _instance.GetMemory("memory")
                ?? throw new InvalidOperationException("wasm did not export 'memory'");
            _externrefTable = _instance.GetTable("__wbindgen_externrefs")
                ?? throw new InvalidOperationException("wasm did not export '__wbindgen_externrefs'");

            _compilePackage = Require("compile_package");
            _writeModule = Require("write_module");
            _writeFile = Require("write_file");
            _writeFileBytes = Require("write_file_bytes");
            _readJs = Require("read_compiled_javascript");
            _readErl = Require("read_compiled_erlang");
            _readFileBytes = Require("read_file_bytes");
            _popWarning = Require("pop_warning");
            _resetWarnings = Require("reset_warnings");
            _resetFilesystem = Require("reset_filesystem");
            _deleteProject = Require("delete_project");
            _malloc = Require("__wbindgen_malloc");
            _free = Require("__wbindgen_free");
            _externrefDealloc = Require("__externref_table_dealloc");

            Require("__wbindgen_start").Invoke();
        }

        private Function Require(string name) =>
            _instance.GetFunction(name) ?? throw new InvalidOperationException($"wasm did not export '{name}'");

        private void DefineImports(Linker linker)
        {
            linker.Define(ImportModule, "__wbg___wbindgen_throw_1506f2235d1bdba0",
                Function.FromCallback(_store, (int ptr, int len) => throw new GleamPanicException(ReadString(ptr, len))));

            linker.Define(ImportModule, "__wbg_error_a6fa202b58aa1cd3",
                Function.FromCallback(_store, (int ptr, int len) =>
                {
                    LogError(ReadString(ptr, len));
                    Free(ptr, len);
                }));

            linker.Define(ImportModule, "__wbg_log_0c201ade58bb55e1",
                Function.FromCallback(_store, (int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7) =>
                {
                    Log(string.Join(" ", ReadString(a0, a1), ReadString(a2, a3), ReadString(a4, a5), ReadString(a6, a7)));
                    Free(a0, a1); Free(a2, a3); Free(a4, a5); Free(a6, a7);
                }));

            linker.Define(ImportModule, "__wbg_log_ce2c4456b290c5e7",
                Function.FromCallback(_store, (int ptr, int len) =>
                {
                    Log(ReadString(ptr, len));
                    Free(ptr, len);
                }));

            linker.Define(ImportModule, "__wbg_mark_b4d943f3bc2d2404",
                Function.FromCallback(_store, (int ptr, int len) => Free(ptr, len)));

            linker.Define(ImportModule, "__wbg_measure_84362959e621a2c1",
                Function.FromCallback(_store, (int a0, int a1, int a2, int a3) => { Free(a0, a1); Free(a2, a3); }));

            // new Error() -> an externref value (read back by __wbg_stack).
            linker.Define(ImportModule, "__wbg_new_227d7c05414eb861",
                Function.FromCallback(_store, () => (object)new WbgError()));

            linker.Define(ImportModule, "__wbg_stack_3b0d974bbf31e44f",
                Function.FromCallback(_store, (int outPtr, object? err) =>
                {
                    var stack = (err as WbgError)?.Stack ?? string.Empty;
                    var (p, l) = AllocString(stack);
                    _memory.WriteInt32(outPtr, p);
                    _memory.WriteInt32(outPtr + 4, l);
                }));

            // Ref(String) -> Externref
            linker.Define(ImportModule, "__wbindgen_cast_0000000000000001",
                Function.FromCallback(_store, (int ptr, int len) => (object)ReadString(ptr, len)));

            linker.Define(ImportModule, "__wbindgen_init_externref_table",
                Function.FromCallback(_store, () =>
                {
                    ulong offset = _externrefTable.Grow(4, null);
                    _externrefTable.SetElement(0, null);
                    _externrefTable.SetElement((uint)offset + 0, null);
                    _externrefTable.SetElement((uint)offset + 1, null);
                    _externrefTable.SetElement((uint)offset + 2, true);
                    _externrefTable.SetElement((uint)offset + 3, false);
                }));
        }

        // ---- public API (mirrors the wasm exports) ----

        public void InitialisePanicHook(bool debug = false) => Require("initialise_panic_hook").Invoke(debug ? 1 : 0);

        public void WriteModule(int projectId, string name, string code)
        {
            var (nPtr, nLen) = AllocString(name);
            var (cPtr, cLen) = AllocString(code);
            _writeModule.Invoke(projectId, nPtr, nLen, cPtr, cLen);
        }

        public void WriteFile(int projectId, string path, string content)
        {
            var (pPtr, pLen) = AllocString(path);
            var (cPtr, cLen) = AllocString(content);
            _writeFile.Invoke(projectId, pPtr, pLen, cPtr, cLen);
        }

        public void WriteFileBytes(int projectId, string path, byte[] content)
        {
            var (pPtr, pLen) = AllocString(path);
            var (cPtr, cLen) = AllocBytes(content);
            _writeFileBytes.Invoke(projectId, pPtr, pLen, cPtr, cLen);
        }

        public void CompilePackage(int projectId, string target)
        {
            var (tPtr, tLen) = AllocString(target);
            var (ret0, ret1) = Invoke2(_compilePackage, projectId, tPtr, tLen);
            if (ret1 != 0)
            {
                var message = TakeExternref(ret0) as string;
                throw new GleamCompileException(message ?? "unknown compile error");
            }
        }

        public string? ReadCompiledJavascript(int projectId, string module)
        {
            var (p, l) = AllocString(module);
            var (ret0, ret1) = Invoke2(_readJs, projectId, p, l);
            return ReadAndFree(ret0, ret1);
        }

        public string? ReadCompiledErlang(int projectId, string module)
        {
            var (p, l) = AllocString(module);
            var (ret0, ret1) = Invoke2(_readErl, projectId, p, l);
            return ReadAndFree(ret0, ret1);
        }

        public byte[]? ReadFileBytes(int projectId, string path)
        {
            var (p, l) = AllocString(path);
            var (ret0, ret1) = Invoke2(_readFileBytes, projectId, p, l);
            if (ret0 == 0) return null;
            var bytes = _memory.GetSpan<byte>(ret0, ret1).ToArray();
            Free(ret0, ret1);
            return bytes;
        }

        public string? PopWarning(int projectId)
        {
            var (ret0, ret1) = Invoke2(_popWarning, projectId);
            return ReadAndFree(ret0, ret1);
        }

        public void ResetWarnings(int projectId) => _resetWarnings.Invoke(projectId);
        public void ResetFilesystem(int projectId) => _resetFilesystem.Invoke(projectId);
        public void DeleteProject(int projectId) => _deleteProject.Invoke(projectId);

        // ---- marshalling helpers ----

        private (int ptr, int len) AllocString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            int ptr = (int)(_malloc.Invoke(bytes.Length, 1) ?? 0);
            var span = _memory.GetSpan<byte>(ptr, bytes.Length);
            bytes.AsSpan().CopyTo(span);
            return (ptr, bytes.Length);
        }

        private (int ptr, int len) AllocBytes(byte[] bytes)
        {
            int ptr = (int)(_malloc.Invoke(bytes.Length, 1) ?? 0);
            var span = _memory.GetSpan<byte>(ptr, bytes.Length);
            bytes.AsSpan().CopyTo(span);
            return (ptr, bytes.Length);
        }

        private string ReadString(int ptr, int len) => _memory.ReadString(ptr, len);

        private void Free(int ptr, int len) => _free.Invoke(ptr, len, 1);

        private string? ReadAndFree(int ptr, int len)
        {
            if (ptr == 0) return null;
            string value = ReadString(ptr, len);
            Free(ptr, len);
            return value;
        }

        private object? TakeExternref(int idx)
        {
            var value = _externrefTable.GetElement((uint)idx);
            _externrefDealloc.Invoke(idx);
            return value;
        }

        /// <summary>Invoke a function returning a two-element multi-value result.</summary>
        private (int, int) Invoke2(Function function, params int[] args)
        {
            var boxes = new ValueBox[args.Length];
            for (int i = 0; i < args.Length; i++) boxes[i] = args[i];

            var result = function.Invoke(boxes);
            if (result is object?[] arr && arr.Length == 2)
                return ((int)arr[0]!, (int)arr[1]!);

            throw new InvalidOperationException($"expected a 2-value wasm result, got '{result?.GetType().Name ?? "null"}'");
        }

        private void Log(string message) => LogSink?.Log(message);
        private void LogError(string message) => LogSink?.Error(message);

        public void Dispose()
        {
            _linker.Dispose();
            _module.Dispose();
            _store.Dispose();
            _engine.Dispose();
        }

        /// <summary>Stand-in for a JS Error object (only `stack` is ever read).</summary>
        private sealed class WbgError
        {
            public string Stack { get; } = "Gleam compiler error";
        }
    }
}