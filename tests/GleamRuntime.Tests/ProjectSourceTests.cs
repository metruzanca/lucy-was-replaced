using System;
using System.IO;
using System.Linq;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    /// <summary>
    /// Tests for compiling from the on-disk Gleam project (the external-editor
    /// mode): project scaffolding discovery, entry resolution and parity with
    /// compiling the same sources in-memory.
    /// </summary>
    [Collection("gleam")]
    public sealed class ProjectSourceTests
    {
        private readonly GleamFixture _fixture;

        public ProjectSourceTests(GleamFixture fixture) => _fixture = fixture;

        private static string TempProject(out string srcDir)
        {
            var dir = Path.Combine(Path.GetTempPath(), "glf-test-" + Guid.NewGuid().ToString("N"));
            srcDir = GleamProjectSource.SrcDir(dir);
            Directory.CreateDirectory(srcDir);
            return dir;
        }

        [Fact]
        public void LoadModulesDiscoversOnlyValidPlayerModules()
        {
            var dir = TempProject(out var src);
            try
            {
                File.WriteAllText(GleamProjectSource.ModuleFile(dir, "main"), "pub fn main() { }");
                File.WriteAllText(GleamProjectSource.ModuleFile(dir, "utils"), "pub fn helper() { }");
                // Reserved / LSP stubs must be excluded from the player module set.
                File.WriteAllText(Path.Combine(src, "game.gleam"), "// stub");
                Directory.CreateDirectory(Path.Combine(src, "game"));
                File.WriteAllText(Path.Combine(src, "game", "item.gleam"), "// stub");
                File.WriteAllText(Path.Combine(src, "bad name.gleam"), "// invalid module name");
                File.WriteAllText(Path.Combine(src, "game_ffi.mjs"), "// not gleam");

                var modules = GleamProjectSource.LoadModules(dir);
                Assert.Equal(new[] { "main", "utils" }, modules.ConvertAll(m => m.Name));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void CompileFromProjectReadsEntryFromDisk()
        {
            var dir = TempProject(out var _);
            try
            {
                File.WriteAllText(
                    GleamProjectSource.ModuleFile(dir, "main"),
                    """
                    import gleam/io
                    import utils

                    pub fn main() {
                      io.println(utils.greet())
                    }
                    """);
                File.WriteAllText(
                    GleamProjectSource.ModuleFile(dir, "utils"),
                    """
                    pub fn greet() -> String {
                      "from disk"
                    }
                    """);

                var compiled = _fixture.Runner.CompileFromProject(dir);
                var sink = new CapturingSink();
                var result = compiled.Run(sink);
                Assert.True(result.IsOk, result.Error?.ToString());
                Assert.Equal("from disk", Assert.Single(sink.Output));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void CompileFromProjectEntrySourceOverridesDisk()
        {
            var dir = TempProject(out var _);
            try
            {
                File.WriteAllText(
                    GleamProjectSource.ModuleFile(dir, "main"),
                    """
                    import gleam/io

                    pub fn main() {
                      io.println("on disk")
                    }
                    """);

                var compiled = _fixture.Runner.CompileFromProject(
                    dir,
                    entrySource: """
                    import gleam/io

                    pub fn main() {
                      io.println("in window")
                    }
                    """);
                var sink = new CapturingSink();
                var result = compiled.Run(sink);
                Assert.True(result.IsOk, result.Error?.ToString());
                Assert.Equal("in window", Assert.Single(sink.Output));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void CompileFromProjectMissingEntryThrows()
        {
            var dir = TempProject(out var _);
            try
            {
                var ex = Assert.Throws<GleamCompileException>(() => _fixture.Runner.CompileFromProject(dir));
                Assert.Contains("main.gleam", ex.Message);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void CompileFromProjectMatchesInMemoryCompile()
        {
            // The on-disk project must compile to the same behaviour as passing the
            // same window sources in-memory — the disk-as-truth guarantee.
            var dir = TempProject(out var _);
            try
            {
                const string main = """
                import gleam/int
                import utils

                pub fn main() {
                  utils.double(int.add(1, 2))
                }
                """;
                const string utils = """
                pub fn double(n: Int) -> Nil {
                  Nil
                }
                """;
                File.WriteAllText(GleamProjectSource.ModuleFile(dir, "main"), main);
                File.WriteAllText(GleamProjectSource.ModuleFile(dir, "utils"), utils);

                var fromProject = _fixture.Runner.CompileFromProject(dir);
                var fromMemory = _fixture.Runner.Compile(
                    main,
                    userModules: new[] { ("utils", utils) });

                Assert.Equal(fromMemory.Sources.Keys.OrderBy(k => k), fromProject.Sources.Keys.OrderBy(k => k));
                Assert.Equal(
                    fromMemory.Run(new CapturingSink()).IsOk,
                    fromProject.Run(new CapturingSink()).IsOk);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}