using System;
using System.Collections.Generic;
using System.IO;

namespace GleamRuntime
{
    /// <summary>
    /// Loads the pinned gleam_stdlib modules, its JS externals and the JS runtime
    /// prelude from a directory (dev) or embedded resources (plugin build).
    /// </summary>
    public static class GleamStdlib
    {
        public const string Version = "1.0.5";

        /// <summary>
        /// Enumerate the stdlib's `.gleam` sources as (module name, source) pairs,
        /// e.g. ("gleam/io", "…"). Nested modules preserve their path.
        /// </summary>
        public static List<(string Name, string Code)> LoadSources(string directory)
        {
            var modules = new List<(string Name, string Code)>();
            foreach (var path in Directory.EnumerateFiles(directory, "*.gleam", SearchOption.AllDirectories))
            {
                var relative = path.Substring(directory.Length).TrimStart('/', '\\');
                var name = relative.Substring(0, relative.Length - ".gleam".Length).Replace('\\', '/');
                modules.Add((name, File.ReadAllText(path)));
            }
            modules.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return modules;
        }

        /// <summary>Read a JS external file (e.g. "gleam_stdlib.mjs", "dict.mjs").</summary>
        public static string LoadExternal(string directory, string fileName) =>
            File.ReadAllText(Path.Combine(directory, fileName));

        /// <summary>Read the JS runtime prelude ("prelude.mjs").</summary>
        public static string LoadPrelude(string directory) =>
            File.ReadAllText(Path.Combine(directory, "prelude.mjs"));
    }
}