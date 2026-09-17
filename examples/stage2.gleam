import game
import game/item
import gleam/io
import gleam/int
import gleam/list

pub fn main() {
  let crops = [item.Carrot, item.Pumpkin, item.Hay]
  let total = list.fold(crops, 0, fn(acc, c) { acc + item.num_items(c) })
  io.println("harvest count: " <> int.to_string(total))
}