import game
import game/item
import gleam/io
import gleam/int

pub fn main() {
  use hay <- with_value(item.num_items(item.Hay))
  io.println("hay: " <> int.to_string(hay))
}

fn with_value(value: Int, continuation: fn(Int) -> Nil) -> Nil {
  continuation(value)
}