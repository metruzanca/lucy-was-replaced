using System;
using System.Collections.Generic;
using System.IO;

namespace GleamRuntime
{
    /// <summary>
    /// Locates and reads the on-disk Gleam project the mod keeps in sync with the
    /// game's code windows. The project lives in a `gleam-project` subfolder of the
    /// save directory (invisible to the game, which only touches top-level `.py`
    /// files) and is a real Gleam package, so players can open it in an external
    /// editor with full Gleam LSP support.
    /// </summary>
    public static class GleamProjectSource
    {
        public const string ProjectDirName = "gleam-project";
        public const string SrcDirName = "src";

        public static string ProjectDir(string saveDir) => Path.Combine(saveDir, ProjectDirName);

        public static string SrcDir(string projectDir) => Path.Combine(projectDir, SrcDirName);

        /// <summary>Path of a module's source file inside the project.</summary>
        public static string ModuleFile(string projectDir, string moduleName) =>
            Path.Combine(SrcDir(projectDir), moduleName + ".gleam");

        /// <summary>
        /// Discover the player's modules in the project's `src/`: every top-level
        /// `*.gleam` whose name is a valid, non-reserved Gleam module name (nested
        /// files such as `game/item.gleam` and the reserved `game.gleam` LSP stub
        /// are excluded — the mod compiles those from its embedded copies).
        /// </summary>
        public static List<(string Name, string Code)> LoadModules(string projectDir)
        {
            var modules = new List<(string Name, string Code)>();
            var srcDir = SrcDir(projectDir);
            if (!Directory.Exists(srcDir)) return modules;

            foreach (var path in Directory.EnumerateFiles(srcDir, "*.gleam", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (!GleamModuleNames.IsValidModuleName(name) || GleamModuleNames.IsReservedName(name))
                    continue;
                modules.Add((name, File.ReadAllText(path)));
            }
            modules.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return modules;
        }

        /// <summary>Read a module's source (or null if the file does not exist).</summary>
        public static string? ReadModule(string projectDir, string moduleName)
        {
            var file = ModuleFile(projectDir, moduleName);
            return File.Exists(file) ? File.ReadAllText(file) : null;
        }
    }
}