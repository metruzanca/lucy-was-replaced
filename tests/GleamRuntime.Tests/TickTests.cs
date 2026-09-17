using System;
using System.Diagnostics;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class TickTests
    {
        private readonly GleamFixture _fixture;

        public TickTests(GleamFixture fixture) => _fixture = fixture;

        private static TickEngine NewTicks() => new();

        [Fact]
        public void StraightLineArithmeticConsumesOps()
        {
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  let a = 1 + 2 * 3 - 4
                  let b = a / 2 + a
                  let _ = a < b
                  Nil
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.PacingEnabled = false;
            var result = compiled.Run(null, null, null, ticks);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.True(ticks.Ops.TotalOps >= 5, $"expected >= 5 ops, got {ticks.Ops.TotalOps}");
        }

        [Fact]
        public void TailRecursionIsPacedLikeALoop()
        {
            // Self tail-recursion compiles to a `while(true)` loop in the JS
            // backend; each iteration should consume ticks like a game loop.
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  countdown(1000)
                }

                fn countdown(n: Int) -> Int {
                  case n {
                    0 -> 0
                    _ -> countdown(n - 1)
                  }
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.PacingEnabled = false;
            var result = compiled.Run(null, null, null, ticks);
            Assert.True(result.IsOk, result.Error?.ToString());
            // ~3 ticks per iteration (branch + comparison + decrement) + start overhead.
            Assert.InRange(ticks.Ops.TotalOps, 2000, 8000);
        }

        [Fact]
        public void NonTailRecursionConsumesOpsPerCall()
        {
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  fib(10)
                }

                fn fib(n: Int) -> Int {
                  case n {
                    0 -> 0
                    1 -> 1
                    _ -> fib(n - 1) + fib(n - 2)
                  }
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.PacingEnabled = false;
            var result = compiled.Run(null, null, null, ticks);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.True(ticks.Ops.TotalOps > 500, $"expected fib(10) to consume many ops, got {ticks.Ops.TotalOps}");
        }

        [Fact]
        public void ActionOpsMergeIntoTotal()
        {
            var compiled = _fixture.Runner.Compile("""
                import game

                pub fn main() {
                  game.move(game.North)
                  game.plant(game.Grass)
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.PacingEnabled = false;
            var bridge = new StubGameBridge { Ops = ticks.Ops };
            var result = compiled.Run(null, null, bridge, ticks);
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.True(
                ticks.Ops.TotalOps >= 400,
                $"expected move+plant (>=400) plus computation, got {ticks.Ops.TotalOps}");
        }

        [Fact]
        public void GetTickCountReflectsConsumedOps()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/int
                import gleam/io
                import game

                pub fn main() {
                  game.move(game.North)
                  game.plant(game.Grass)
                  io.println(int.to_string(game.get_tick_count()))
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.PacingEnabled = false;
            var bridge = new StubGameBridge { Ops = ticks.Ops };
            var sink = new CapturingSink();
            var result = compiled.Run(sink, null, bridge, ticks);
            Assert.True(result.IsOk, result.Error?.ToString());
            var printed = long.Parse(Assert.Single(sink.Output));
            Assert.True(printed >= 400, $"get_tick_count printed {printed}, expected >= 400");
        }

        [Fact]
        public void PacingSlepsToRealTime()
        {
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  countdown(300)
                }

                fn countdown(n: Int) -> Int {
                  case n {
                    0 -> 0
                    _ -> countdown(n - 1)
                  }
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.OpDurationSeconds = 0.001; // 1000 ops/s
            var sw = Stopwatch.StartNew();
            var result = compiled.Run(null, TimeSpan.FromSeconds(30), null, ticks);
            sw.Stop();
            Assert.True(result.IsOk, result.Error?.ToString());
            // ~900+ ops at 1 ms each => at least ~0.9 s of real time.
            Assert.True(sw.ElapsedMilliseconds >= 700, $"paced run finished in {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void DisabledPacingRunsFast()
        {
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  countdown(10000)
                }

                fn countdown(n: Int) -> Int {
                  case n {
                    0 -> 0
                    _ -> countdown(n - 1)
                  }
                }
                """);
            var ticks = NewTicks();
            ticks.Pacer.PacingEnabled = false;
            var sw = Stopwatch.StartNew();
            var result = compiled.Run(null, null, null, ticks);
            sw.Stop();
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.True(sw.ElapsedMilliseconds < 2000, $"unpaced countdown(10000) took {sw.ElapsedMilliseconds}ms");
        }
    }
}