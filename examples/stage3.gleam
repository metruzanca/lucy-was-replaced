import game
import game/item
import gleam/io
import gleam/int
import gleam/list

pub fn main() {
  let hay = item.num_items(item.Hay)
  let report = hay |> int.to_string |> fn(s) { "hay: " <> s }
  io.println(report)

  let squares = list.map([1, 2, 3], fn(x) { x * x })
  io.println(int.to_string(list.length(squares)))
}