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
        public void GameModuleRoutesToBridge()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/int
                import gleam/option.{Some}
                import game
                import game/item

                pub fn main() {
                  game.move(game.North)
                  game.plant(game.Grass)
                  game.till()
                  let pos = game.get_pos()
                  io.println(int.to_string(pos.x))
                  case game.get_entity_type() {
                    Some(game.Grass) -> io.println("grass")
                    _ -> io.println("other")
                  }
                  item.num_items(item.Hay)
                }
                """);

            var bridge = new StubGameBridge();
            var sink = new CapturingSink();
            var result = compiled.Run(sink, null, bridge);

            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Contains("move(0)", bridge.Calls);       // North
            Assert.Contains("plant(1)", bridge.Calls);      // Grass
            Assert.Contains("till()", bridge.Calls);
            Assert.Contains("get_pos_x()", bridge.Calls);
            Assert.Contains("get_pos_y()", bridge.Calls);
            Assert.Contains("get_entity_type()", bridge.Calls);
            Assert.Contains("num_items(1)", bridge.Calls);  // Hay
            Assert.Contains("grass", sink.Output);          // stub returns entity code 1 -> Some(Grass)
        }

        [Fact]
        public void UtilityBuiltinsRouteToBridge()
        {
            var compiled = _fixture.Runner.Compile("""
                import game
                import game/item as item

                pub fn main() {
                  game.swap(game.East)
                  game.clear()
                  game.get_pos()
                  game.measure()
                  game.measure_at(game.North)
                  game.get_companion()
                  game.get_cost(game.Carrot)
                  game.random()
                  game.num_drones()
                  game.max_drones()
                  game.unlock(game.Carrot)
                  game.num_unlocked(game.Carrot)
                  game.unlock_by_name("multi_trade")
                  game.num_unlocked_by_name("multi_trade")
                  game.set_execution_speed(1.0)
                  game.set_world_size(4)
                  game.do_a_flip()
                  game.pet_the_piggy()
                  game.change_hat("sombrero")
                  game.quick_print("hi")
                  item.use_items(item.Water, 3)
                  item.unlock_item(item.Hay)
                  item.num_unlocked_item(item.Water)
                }
                """);

            var bridge = new StubGameBridge();
            var sink = new CapturingSink();
            var result = compiled.Run(sink, null, bridge);

            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Contains("swap(1)", bridge.Calls);          // East
            Assert.Contains("clear()", bridge.Calls);
            Assert.Contains("get_pos_x()", bridge.Calls);
            Assert.Contains("get_pos_y()", bridge.Calls);
            Assert.Contains("measure()", bridge.Calls);
            Assert.Contains("measure_at(0)", bridge.Calls);    // North
            Assert.Contains("get_companion()", bridge.Calls);
            Assert.Contains("get_cost(3)", bridge.Calls);      // Carrot
            Assert.Contains("random()", bridge.Calls);
            Assert.Contains("num_drones()", bridge.Calls);
            Assert.Contains("max_drones()", bridge.Calls);
            Assert.Contains("unlock(carrot)", bridge.Calls);
            Assert.Contains("num_unlocked(carrot)", bridge.Calls);
            Assert.Contains("unlock(multi_trade)", bridge.Calls);
            Assert.Contains("num_unlocked(multi_trade)", bridge.Calls);
            Assert.Contains("set_execution_speed(1)", bridge.Calls);
            Assert.Contains("set_world_size(4)", bridge.Calls);
            Assert.Contains("do_a_flip()", bridge.Calls);
            Assert.Contains("pet_the_piggy()", bridge.Calls);
            Assert.Contains("change_hat(sombrero)", bridge.Calls);
            Assert.Contains("quick_print(hi)", bridge.Calls);
            Assert.Contains("use_item(8, 3)", bridge.Calls);   // Water x3
            Assert.Contains("unlock(hay)", bridge.Calls);
            Assert.Contains("num_unlocked(water)", bridge.Calls);
        }

        [Fact]
        public void UtilityBuiltinsDecodeTypedValues()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/float
                import gleam/int
                import gleam/list
                import gleam/option.{None, Some}
                import game

                pub fn main() {
                  case game.measure() {
                    Some(v) -> io.println("measure:" <> float.to_string(v))
                    None -> io.println("measure:none")
                  }
                  case game.get_companion() {
                    Some(companion) -> io.println("companion:yes")
                    None -> io.println("companion:none")
                  }
                  let pos = game.get_pos()
                  io.println("pos:" <> int.to_string(pos.x) <> "," <> int.to_string(pos.y))
                  io.println("cost:" <> int.to_string(list.length(game.get_cost(game.Carrot))))
                  io.println("random:" <> float.to_string(game.random()))
                }
                """);

            var bridge = new StubGameBridge();
            var sink = new CapturingSink();
            var result = compiled.Run(sink, null, bridge);

            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal(
                new[] { "measure:1.5", "companion:none", "pos:0,0", "cost:2", "random:0.5" },
                sink.Output);
        }

        [Fact]
        public void CompanionRecordDecodesEntityAndPosition()
        {
            var compiled = _fixture.Runner.Compile("""
                import gleam/io
                import gleam/int
                import gleam/option.{None, Some}
                import game

                pub fn main() {
                  case game.get_companion() {
                    Some(companion) -> io.println(
                      "companion:" <>
                      int.to_string(game.entity_code(companion.entity)) <>
                      "@" <>
                      int.to_string(companion.position.x) <>
                      "," <>
                      int.to_string(companion.position.y),
                    )
                    None -> io.println("companion:none")
                  }
                }
                """);

            var bridge = new StubGameBridge { CompanionResult = new[] { 3, 2, 5 } };
            var sink = new CapturingSink();
            var result = compiled.Run(sink, null, bridge);

            Assert.True(result.IsOk, result.Error?.ToString());
            Assert.Equal("companion:3@2,5", Assert.Single(sink.Output)); // Carrot at (2, 5)
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