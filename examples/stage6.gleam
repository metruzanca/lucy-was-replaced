import gleam/io
import gleam/int
import gleam/list

pub fn main() {
  let evens = list.filter([1, 2, 3, 4, 5, 6], fn(x) { x % 2 == 0 })
  io.println(int.to_string(list.length(evens)))
}