using System;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class GleamErrorsTests
    {
        private readonly GleamFixture _fixture;

        public GleamErrorsTests(GleamFixture fixture) => _fixture = fixture;

        [Fact]
        public void TodoErrorIsDescribedFriendly()
        {
            var compiled = _fixture.Runner.Compile("""
                pub fn main() {
                  try_harvest()
                }

                fn try_harvest() {
                  do_work(fn() {})
                }

                fn do_work(func: fn() -> Bool) -> Bool {
                  case True {
                    True -> {
                      func()
                      False
                    }
                    _ -> False
                  }
                }
                """);

            var result = compiled.Run(null, TimeSpan.FromSeconds(10));
            Assert.False(result.IsOk);
            Assert.True(GleamErrors.IsTodo(result.Error!));

            var message = GleamErrors.Describe(result.Error!);
            Assert.Contains("unfinished function", message);
            Assert.Contains("`todo` placeholder", message);
            Assert.Contains("at func (main:", message);   // JS call site, not the Jint stack
            Assert.DoesNotContain("Jint.Runtime.Throw", message);
            Assert.DoesNotContain("EvaluateModule", message);
        }

        [Fact]
        public void TodoEntryPointIsDescribedFriendly()
        {
            // The simplest case: the entry function itself has an empty body.
            var compiled = _fixture.Runner.Compile("pub fn main() {}");

            var result = compiled.Run(null, TimeSpan.FromSeconds(10));
            Assert.False(result.IsOk);
            Assert.True(GleamErrors.IsTodo(result.Error!));
            Assert.Contains("unfinished function", GleamErrors.Describe(result.Error!));
        }

        [Fact]
        public void NonTodoErrorFallsThroughToRawText()
        {
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
            Assert.False(GleamErrors.IsTodo(result.Error!));
            Assert.Equal(result.Error!.ToString(), GleamErrors.Describe(result.Error!));
        }
    }
}