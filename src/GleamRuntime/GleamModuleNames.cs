using System;
using System.Text.RegularExpressions;

namespace GleamRuntime
{
    /// <summary>
    /// Rules for turning the game's code windows into importable Gleam modules.
    /// Windows whose names can't be Gleam module names (or that would shadow the
    /// mod's own modules) are left out of the compiled project — exactly as the
    /// game's Python would refuse to import a window with an invalid module name.
    /// </summary>
    public static class GleamModuleNames
    {
        private static readonly Regex ModuleName = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>Names the mod reserves for its own compiled modules.</summary>
        private static readonly System.Collections.Generic.HashSet<string> Reserved = new(
            new[]
            {
                "game", "game_ffi", "gleam", "gleam_stdlib", "dict",
            },
            StringComparer.Ordinal);

        public static bool IsValidModuleName(string? name) =>
            !string.IsNullOrEmpty(name) && ModuleName.IsMatch(name);

        public static bool IsReservedName(string name) => Reserved.Contains(name);
    }
}