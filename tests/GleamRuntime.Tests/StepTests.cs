using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class StepTests
    {
        private readonly GleamFixture _fixture;

        public StepTests(GleamFixture fixture) => _fixture = fixture;

        private sealed class StepProbe : IGleamLineSink
        {
            private readonly TaskCompletionSource<int> _first = new();
            public List<int> Lines { get; } = new();

            public Task FirstLine => _first.Task;

            public void OnLine(string module, int gleamLine)
            {
                lock (Lines) Lines.Add(gleamLine);
                _first.TrySetResult(gleamLine);
            }
        }

        [Fact]
        public async Task StepModePausesAndAdvancesPerLine()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io

                pub fn main() {
                  io.println("one")
                  io.println("two")
                  io.println("three")
                  Nil
                }
                """);
            var ticks = new TickEngine { };
            ticks.Pacer.PacingEnabled = false;
            ticks.StepGate.Enter();
            var probe = new StepProbe();
            var sink = new CapturingSink();

            var run = Task.Run(() => compiled.Run(sink, TimeSpan.FromSeconds(30), null, ticks, lineSink: probe));

            // The first line reports, then execution pauses at the next new line.
            await probe.FirstLine.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(150);
            Assert.False(run.IsCompleted, "run should be paused in step mode");

            // Each Next advances one more line; the program finishes after the last.
            for (var i = 0; i < 5; i++)
            {
                if (run.IsCompleted) break;
                ticks.StepGate.Next();
                await Task.Delay(100);
            }

            var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.True(probe.Lines.Count >= 4, $"expected the body lines to be reported, got {probe.Lines.Count}");
        }

        [Fact]
        public async Task AbortReleasesABlockedStep()
        {
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  let a = 1
                  let b = 2
                  countdown(1000)
                  Nil
                }

                fn countdown(n: Int) -> Int {
                  case n {
                    0 -> 0
                    _ -> countdown(n - 1)
                  }
                }
                """);
            var ticks = new TickEngine { };
            ticks.Pacer.PacingEnabled = false;
            ticks.StepGate.Enter();
            var probe = new StepProbe();
            var sink = new CapturingSink();

            var run = Task.Run(() => compiled.Run(sink, TimeSpan.FromSeconds(30), null, ticks, lineSink: probe));
            await probe.FirstLine.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(150);
            Assert.False(run.IsCompleted, "run should be paused in step mode");

            // Abort (stop) must release the blocked engine; the run unwinds as a stop.
            ticks.StepGate.Abort();
            var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(result.IsOk);
            Assert.IsType<GleamStoppedException>(result.Error);
        }
    }
}