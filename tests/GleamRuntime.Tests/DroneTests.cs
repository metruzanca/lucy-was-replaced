using System;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class DroneTests
    {
        private readonly GleamFixture _fixture;

        public DroneTests(GleamFixture fixture) => _fixture = fixture;

        private static TickEngine NewTicks()
        {
            var ticks = new TickEngine();
            ticks.Pacer.PacingEnabled = false;
            return ticks;
        }

        private static void AssertOk(GleamRunResult result) =>
            Assert.True(result.IsOk, result.Error?.ToString());

        [Fact]
        public void SpawnedWorkerRunsAndReturnsResult()
        {
            var compiled = _fixture.Runner.Compile("""
                import game
                import gleam/dynamic.{type Dynamic}
                import gleam/dynamic/decode
                import gleam/int
                import gleam/io
                import gleam/option.{None, Some}

                pub fn main() {
                  let handles = game.spawn_drone_with(1, worker)
                  case handles {
                    [h, ..] -> case game.wait_for(h) {
                      Some(value) -> case decode.run(value, decode.int) {
                        Ok(n) -> io.println(int.to_string(n))
                        Error(_) -> io.println("bad")
                      }
                      None -> io.println("none")
                    }
                    _ -> io.println("no_handles")
                  }
                }

                pub fn worker() -> Dynamic {
                  dynamic.int(42)
                }
                """);

            var sink = new CapturingSink();
            var result = compiled.Run(sink, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            AssertOk(result);
            Assert.Equal("42", Assert.Single(sink.Output));
        }

        [Fact]
        public void SpawnedDronesRunConcurrently()
        {
            var compiled = _fixture.Runner.Compile("""
                import game
                import gleam/dynamic.{type Dynamic}
                import gleam/dynamic/decode
                import gleam/int
                import gleam/io
                import gleam/option.{None, Some}

                pub fn main() {
                  let handles = game.spawn_drone_with(3, worker_id)
                  case handles {
                    [a, b, c, ..] -> {
                      await_and_print(a)
                      await_and_print(b)
                      await_and_print(c)
                    }
                    _ -> io.println("no_handles")
                  }
                }

                fn await_and_print(h: game.DroneHandle) {
                  case game.wait_for(h) {
                    Some(value) -> case decode.run(value, decode.int) {
                      Ok(n) -> io.println(int.to_string(n))
                      Error(_) -> io.println("bad")
                    }
                    None -> io.println("none")
                  }
                }

                pub fn worker_id() -> Dynamic {
                  dynamic.int(game.get_drone_id())
                }
                """);

            var sink = new CapturingSink();
            var result = compiled.Run(sink, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            AssertOk(result);
            Assert.Equal(new[] { "1", "2", "3" }, sink.Output);
        }

        [Fact]
        public void SendAndReceiveDeliverMessage()
        {
            var compiled = _fixture.Runner.Compile("""
                import game
                import gleam/dynamic
                import gleam/dynamic/decode
                import gleam/int
                import gleam/io
                import gleam/option.{None, Some}

                pub fn main() {
                  let handles = game.spawn_drone(1, sender)
                  case handles {
                    [h, ..] -> {
                      let _ = game.wait_for(h)
                      case game.receive() {
                        Some(value) -> case decode.run(value, decode.int) {
                          Ok(n) -> io.println("got:" <> int.to_string(n))
                          Error(_) -> io.println("got:bad")
                        }
                        None -> io.println("none")
                      }
                    }
                    _ -> io.println("no_handles")
                  }
                }

                pub fn sender() -> Nil {
                  game.send(dynamic.int(99), 0)
                }
                """);

            var sink = new CapturingSink();
            var result = compiled.Run(sink, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            AssertOk(result);
            Assert.Equal("got:99", Assert.Single(sink.Output));
        }

        [Fact]
        public void HasFinishedTracksCompletion()
        {
            var compiled = _fixture.Runner.Compile("""
                import game
                import gleam/dynamic
                import gleam/io
                import gleam/option.{None, Some}

                pub fn main() {
                  let handles = game.spawn_drone(1, worker)
                  case handles {
                    [h, ..] -> {
                      case game.has_finished(h) {
                        True -> io.println("true")
                        False -> io.println("false")
                      }
                      game.send(dynamic.int(1), 1)
                      let _ = game.wait_for(h)
                      case game.has_finished(h) {
                        True -> io.println("true")
                        False -> io.println("false")
                      }
                    }
                    _ -> io.println("no_handles")
                  }
                }

                pub fn worker() -> Nil {
                  poll()
                }

                fn poll() -> Nil {
                  case game.receive() {
                    Some(_) -> Nil
                    None -> poll()
                  }
                }
                """);

            var sink = new CapturingSink();
            var result = compiled.Run(sink, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            AssertOk(result);
            Assert.Equal(new[] { "false", "true" }, sink.Output);
        }

        [Fact]
        public void AnonymousWorkerFailsWithClearError()
        {
            var compiled = _fixture.Runner.Compile("""
                import game

                pub fn main() {
                  let _ = game.spawn_drone(1, fn() { Nil })
                  Nil
                }
                """);

            var result = compiled.Run(null, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            Assert.False(result.IsOk);
            Assert.Contains("must be a named", result.Error?.Message ?? "");
        }

        [Fact]
        public void WaitForMissingHandleIsNone()
        {
            var compiled = _fixture.Runner.Compile("""
                import game
                import gleam/io
                import gleam/option.{None, Some}

                pub fn main() {
                  case game.wait_for(game.DroneHandle(99, 99)) {
                    Some(_) -> io.println("some")
                    None -> io.println("none")
                  }
                }
                """);

            var sink = new CapturingSink();
            var result = compiled.Run(sink, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            AssertOk(result);
            Assert.Equal("none", Assert.Single(sink.Output));
        }

        [Fact]
        public void InfiniteWorkerStopsWhenMainFinishes()
        {
            var compiled = _fixture.Runner.Compile("""
                import game

                pub fn main() {
                  let _ = game.spawn_drone(1, forever)
                  Nil
                }

                pub fn forever() -> Nil {
                  forever()
                }
                """);

            var result = compiled.Run(null, TimeSpan.FromSeconds(30), ticks: NewTicks(), enableDrones: true);
            AssertOk(result);
        }
    }
}