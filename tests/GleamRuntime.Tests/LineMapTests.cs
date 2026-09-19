using System.Collections.Generic;
using System.Linq;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class LineMapTests
    {
        private readonly GleamFixture _fixture;

        public LineMapTests(GleamFixture fixture) => _fixture = fixture;

        /// <summary>Runs the program headless and records (module, gleamLine) per executed step.</summary>
        private List<string> RunLines(string source, string entryModule = "main")
        {
            var compiled = _fixture.Runner.Compile(source, entryModule);
            var recorder = new LineRecorder();
            var ticks = new TickEngine { };
            ticks.Pacer.PacingEnabled = false;
            var result = compiled.Run(null, null, null, ticks, lineSink: recorder);
            Assert.True(result.IsOk, result.Error?.ToString());
            return recorder.Sequence;
        }

        [Fact]
        public void StraightLineProgramHitsItsOwnLines()
        {
            var seq = RunLines("""
                pub fn main() {
                  let a = 1 + 2 * 3 - 4
                  let b = a / 2 + a
                  let _ = a < b
                  Nil
                }
                """);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, seq);
        }

        [Fact]
        public void CaseHighlightsTakenBranchOnly()
        {
            var seq = RunLines("""
                import gleam/io

                pub fn main() {
                  let total = 15
                  case total {
                    15 -> io.println("fifteen")
                    _ -> io.println("other")
                  }
                  io.println("done")
                  Nil
                }
                """);
            Assert.Equal(new[] { "3", "4", "5", "6", "9", "10" }, seq);
        }

        [Fact]
        public void RecursionHighlightsCaseAndClauseLines()
        {
            var seq = RunLines("""
                pub fn main() {
                  countdown(3)
                }

                fn countdown(n: Int) -> Int {
                  case n {
                    0 -> 0
                    _ -> countdown(n - 1)
                  }
                }
                """);
            Assert.Equal(
                new[] { "5", "1", "2", "6", "7", "8", "6", "7", "8", "6", "7", "8", "6", "7" }, seq);
        }

        [Fact]
        public void UseContinuationResumesOnFollowingLines()
        {
            var seq = RunLines("""
                import gleam/int
                import gleam/io

                pub fn main() {
                  use n <- with_value(42)
                  io.println(int.to_string(n))
                  Nil
                }

                fn with_value(n: Int, continuation: fn(Int) -> Nil) -> Nil {
                  continuation(n)
                }
                """);
            Assert.Equal(new[] { "4", "10", "4", "5", "11", "6", "7" }, seq);
        }

        [Fact]
        public void InlineClosureStaysOnItsStatementLine()
        {
            var seq = RunLines("""
                import gleam/io

                pub fn main() {
                  let f = fn(a: Int) { a * 2 }
                  let doubled = f(21)
                  io.println("done")
                  Nil
                }
                """);
            Assert.Equal(new[] { "3", "4", "5", "4", "6", "7" }, seq);
        }

        [Fact]
        public void MultiLineClosureBodyHitsItsOwnLine()
        {
            var seq = RunLines("""
                import gleam/io

                pub fn main() {
                  let total = apply(10, fn(x) {
                    x + 5
                  })
                  io.println("done")
                  Nil
                }

                fn apply(x: Int, f: fn(Int) -> Int) -> Int {
                  f(x)
                }
                """);
            Assert.Equal(new[] { "3", "11", "3", "4", "12", "5", "7", "8" }, seq);
        }

        [Fact]
        public void LineMapsCoverTheEntryModule()
        {
            var compiled = _fixture.Runner.Compile("pub fn main() {\n  Nil\n}\n");
            Assert.True(compiled.LineMaps.TryGetValue("main", out var map));
            Assert.True(map.Count > 0);
        }

        [Fact]
        public void UserModulesGetLineMapsToo()
        {
            var compiled = _fixture.Runner.Compile(
                "import helper\n\npub fn main() {\n  helper.answer()\n  Nil\n}\n",
                "main",
                new[] { ("helper", "pub fn answer() -> Int {\n  42\n}\n") });
            Assert.True(compiled.LineMaps.ContainsKey("main"));
            Assert.True(compiled.LineMaps.TryGetValue("helper", out var helperMap));
            Assert.True(helperMap.Count > 0);
        }

        private sealed class LineRecorder : IGleamLineSink
        {
            private readonly List<string> _lines = new();
            private string _last = "";

            public List<string> Sequence => _lines;

            public void OnLine(string module, int gleamLine)
            {
                var key = gleamLine.ToString();
                if (key == _last) return;
                _last = key;
                _lines.Add(key);
            }
        }
    }
}