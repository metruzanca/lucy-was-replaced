using System.Collections.Generic;
using System.Linq;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    [Collection("gleam")]
    public sealed class StatementScannerTests
    {
        private static List<int> Lines(string source)
            => GleamStatementScanner.Scan(source).Select(s => s.Line).ToList();

        [Fact]
        public void StraightLineStatementsAreRecorded()
        {
            var lines = Lines("""
                pub fn main() {
                  let a = 1 + 2 * 3 - 4
                  let b = a / 2 + a
                  let _ = a < b
                  Nil
                }
                """);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, lines);
        }

        [Fact]
        public void MultiLineCallCountsOnce()
        {
            var lines = Lines("""
                pub fn main() {
                  io.println(
                    "a" <> "b",
                  )
                  let x = 1
                  Nil
                }
                """);
            Assert.Equal(new[] { 1, 2, 5, 6 }, lines);
        }

        [Fact]
        public void CaseClausesAreRecorded()
        {
            var lines = Lines("""
                pub fn main() {
                  let n = 5
                  case n {
                    0 -> io.println("zero")
                    5 -> io.println("five")
                    _ -> io.println("other")
                  }
                  Nil
                }
                """);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 8 }, lines);
        }

        [Fact]
        public void UseAndMultiLineClosureBodyAreRecorded()
        {
            var lines = Lines("""
                pub fn main() {
                  let total = fold([1, 2, 3], 0, fn(a, b) {
                    a + b
                  })
                  use n <- with_value(42)
                  io.println(int.to_string(n))
                  Nil
                }
                fn with_value(n: Int, continuation: fn(Int) -> Nil) -> Nil {
                  continuation(n)
                }
                """);
            Assert.Equal(new[] { 1, 2, 3, 5, 6, 7, 9, 10 }, lines);
        }

        [Fact]
        public void PipelineAndMultiClauseBodiesAreRecorded()
        {
            var lines = Lines("""
                pub fn main() {
                  let pi = 1
                    |> add_one
                    |> add_one
                  case pi {
                    0 -> {
                      io.println("zero")
                    }
                    _ -> io.println("other")
                  }
                  Nil
                }
                fn add_one(n: Int) -> Int {
                  n + 1
                }
                """);
            Assert.Equal(new[] { 1, 2, 5, 6, 7, 9, 11, 13, 14 }, lines);
        }

        [Fact]
        public void CommentsBlankLinesAndStringsDoNotDisturb()
        {
            var lines = Lines("""
                import gleam/io

                // a comment
                pub fn main() {
                  // another
                  io.println(&QQ;
                    body
                  &QQ;)
                  let xs = [1, 2] // trailing comment
                  Nil
                }
                """.Replace("&QQ;", "\"\"\""));
            Assert.Equal(new[] { 4, 6, 9, 10 }, lines);
        }

        [Fact]
        public void FunctionSignaturesAreTagged()
        {
            var scanned = GleamStatementScanner.Scan("""
                pub fn main() {
                  Nil
                }

                fn helper(n: Int) -> Int {
                  n
                }
                """);
            Assert.Equal(new[] { "main", "helper" },
                scanned.Where(s => s.IsFunctionSignature).Select(s => s.FunctionName));
        }

        [Fact]
        public void ClosuresAreTaggedWithDepthAndInline()
        {
            var scanned = GleamStatementScanner.Scan("""
                pub fn main() {
                  let inline = fn(a) { a * 2 }
                  let multi = fn(a) {
                    a + 1
                  }
                  Nil
                }
                """);
            var inline = scanned[1].Closures.Single();
            Assert.True(inline.Inline);
            Assert.Equal(2, inline.Depth);

            var multi = scanned[2].Closures.Single();
            Assert.False(multi.Inline);
            Assert.Equal(2, multi.Depth);
        }
    }
}