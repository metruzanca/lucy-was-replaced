using System;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class ModuleTests
    {
        private readonly GleamFixture _fixture;

        public ModuleTests(GleamFixture fixture) => _fixture = fixture;

        [Fact]
        public void ImportUserModuleCompilesAndRuns()
        {
            var compiled = _fixture.Runner.Compile(
                """
                import gleam/io
                import utils

                pub fn main() {
                  io.println("ran")
                  utils.visit_all()
                }
                """,
                userModules: new[]
                {
                    ("utils", """
                    pub fn visit_all() -> Nil {
                      Nil
                    }
                    """),
                });

            var sink = new CapturingSink();
            var result = compiled.Run(sink);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal("ran", Assert.Single(sink.Output));
        }

        [Fact]
        public void NonDefaultEntryModuleNameRuns()
        {
            var compiled = _fixture.Runner.Compile(
                """
                import gleam/io

                pub fn main() {
                  io.println("hi")
                }
                """,
                entryModuleName: "worker");

            var sink = new CapturingSink();
            var result = compiled.Run(sink);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal("hi", Assert.Single(sink.Output));
        }

        [Fact]
        public void ImportingPrivateFunctionIsACompileError()
        {
            // A module's private functions are not callable across modules; the compiler
            // must surface that (the player then adds `pub` to the helper).
            var ex = Assert.Throws<GleamCompileException>(() =>
                _fixture.Runner.Compile(
                    """
                    import utils

                    pub fn main() {
                      utils.visit_all()
                    }
                    """,
                    userModules: new[]
                    {
                        ("utils", """
                        fn visit_all() -> Nil {
                          Nil
                        }
                        """),
                    }));

            Assert.Contains("utils", ex.Message);
        }

        [Fact]
        public void ModuleNameValidation()
        {
            Assert.True(GleamModuleNames.IsValidModuleName("utils"));
            Assert.True(GleamModuleNames.IsValidModuleName("farm_helper2"));
            Assert.False(GleamModuleNames.IsValidModuleName("My Helper"));
            Assert.False(GleamModuleNames.IsValidModuleName("my-helper"));
            Assert.False(GleamModuleNames.IsValidModuleName("2farm"));
            Assert.False(GleamModuleNames.IsValidModuleName(""));
            Assert.False(GleamModuleNames.IsValidModuleName(null));

            Assert.True(GleamModuleNames.IsReservedName("game"));
            Assert.True(GleamModuleNames.IsReservedName("gleam_stdlib"));
            Assert.False(GleamModuleNames.IsReservedName("utils"));
        }
    }
}