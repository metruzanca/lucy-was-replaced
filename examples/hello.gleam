import gleam/int
import gleam/io
import gleam/list
import gleam/string

pub fn main() {
  let xs = [1, 2, 3, 4, 5]
  let total = list.fold(xs, 0, fn(acc, x) { acc + x })
  io.println("hello from gleam!")
  io.println("total: " <> int.to_string(total))
  io.println("strings: " <> string.join(["a", "b", "c"], ", "))
}
