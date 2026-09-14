using System;
using System.Collections.Generic;

namespace GleamRuntime
{
    /// <summary>
    /// High-level Gleam compiler: creates projects, compiles packages, reads outputs.
    /// Port of the Gleam playground's `static/compiler.js` wrapper.
    /// </summary>
    public sealed class GleamCompiler : IDisposable
    {
        private readonly GleamWasm _wasm;
        private readonly object _lock = new object();
        private int _nextId;

        public GleamCompiler(byte[] wasmBytes)
        {
            _wasm = new GleamWasm(wasmBytes);
            _wasm.InitialisePanicHook(false);
        }

        public IGleamLogSink? LogSink
        {
            get => _wasm.LogSink;
            set => _wasm.LogSink = value;
        }

        public GleamProject NewProject()
        {
            lock (_lock)
            {
                return new GleamProject(_wasm, _nextId++);
            }
        }

        public void Dispose() => _wasm.Dispose();
    }

    /// <summary>A single compilation project (one virtual package).</summary>
    public sealed class GleamProject : IDisposable
    {
        private readonly GleamWasm _wasm;
        private readonly int _id;

        internal GleamProject(GleamWasm wasm, int id)
        {
            _wasm = wasm;
            _id = id;
        }

        public int ProjectId => _id;

        /// <summary>Write a Gleam module (module name without `.gleam`).</summary>
        public void WriteModule(string moduleName, string code) => _wasm.WriteModule(_id, moduleName, code);

        /// <summary>Write an arbitrary file into the virtual filesystem.</summary>
        public void WriteFile(string path, string content) => _wasm.WriteFile(_id, path, content);

        public void WriteFileBytes(string path, byte[] content) => _wasm.WriteFileBytes(_id, path, content);

        /// <summary>Compile the package for the given target ("javascript" or "erlang").</summary>
        /// <exception cref="GleamCompileException">The package failed to compile.</exception>
        public void CompilePackage(string target)
        {
            _wasm.ResetWarnings(_id);
            _wasm.CompilePackage(_id, target);
        }

        public string? ReadCompiledJavascript(string moduleName) => _wasm.ReadCompiledJavascript(_id, moduleName);

        public string? ReadCompiledErlang(string moduleName) => _wasm.ReadCompiledErlang(_id, moduleName);

        public byte[]? ReadFileBytes(string path) => _wasm.ReadFileBytes(_id, path);

        public void ResetFilesystem() => _wasm.ResetFilesystem(_id);

        /// <summary>Drain all compiler warnings accumulated since the last compile.</summary>
        public List<string> TakeWarnings()
        {
            var warnings = new List<string>();
            while (true)
            {
                var warning = _wasm.PopWarning(_id);
                if (warning == null) return warnings;
                warnings.Add(warning.TrimStart());
            }
        }

        public void Dispose() => _wasm.DeleteProject(_id);
    }
}