using System;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class CompileRunTests
    {
        private readonly GleamFixture _fixture;

        public CompileRunTests(GleamFixture fixture) => _fixture = fixture;

        [Fact]
        public void HelloCompilesAndRuns()
        {
            var compiled = _fixture.Runner.Compile("pub fn main() { Nil }");
            var result = compiled.Run();
            Assert.True(result.IsOk, result.Error?.ToString());
        }

        [Fact]
        public void StdlibRunsAndPrints()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/list
                import gleam/int

                pub fn main() {
                  io.println(int.to_string(list.length([1, 2, 3])))
                }
                """);
            var sink = new CapturingSink();
            var result = compiled.Run(sink);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal("3", Assert.Single(sink.Output));
        }

        [Fact]
        public void RecordsAndRecordUpdateWork()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/int

                pub type Point {
                  Point(x: Int, y: Int)
                }

                pub fn main() {
                  let p = Point(3, 4)
                  let q = Point(..p, x: 10)
                  io.println(int.to_string(q.x) <> "," <> int.to_string(q.y))
                }
                """);
            var sink = new CapturingSink();
            var result = compiled.Run(sink);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal("10,4", Assert.Single(sink.Output));
        }

        [Fact]
        public void UseExpressionWorks()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/int

                pub fn main() {
                  use n <- with_value(42)
                  io.println(int.to_string(n))
                }

                fn with_value(n: Int, continuation: fn(Int) -> Nil) -> Nil {
                  continuation(n)
                }
                """);
            var sink = new CapturingSink();
            var result = compiled.Run(sink);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal("42", Assert.Single(sink.Output));
        }

        [Fact]
        public void TfwrFfiStubRoutesToConsole()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/int
                import tfwr

                pub fn main() {
                  tfwr.harvest()
                  tfwr.move(tfwr.east)
                  io.println("pos: " <> int.to_string(tfwr.get_pos_x()))
                }
                """);
            var sink = new CapturingSink();
            var result = compiled.Run(sink);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Contains("[tfwr] harvest()", sink.Output);
            Assert.Contains("[tfwr] move(1)", sink.Output);
            Assert.Contains("pos: 0", sink.Output);
        }

        [Fact]
        public void CompileErrorReportsGleamLocation()
        {
            var ex = Assert.Throws<GleamCompileException>(() =>
                _fixture.Runner.Compile("pub fn main() { let x = "));

            Assert.Contains("src/main.gleam:1", ex.Message);
            Assert.Contains("Syntax error", ex.Message);
        }

        [Fact]
        public void InfiniteLoopIsStoppedByTimeout()
        {
            // Gleam has no loops: this is non-tail recursion, which the JS backend
            // keeps recursive, so it hits the Jint recursion limit or timeout.
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  loop(0)
                }

                fn loop(n: Int) -> Int {
                  loop(n + 1)
                }
                """);

            var result = compiled.Run(null, TimeSpan.FromMilliseconds(300));
            Assert.False(result.IsOk);
        }
    }
}