import game
import game/item
import gleam/io
import gleam/int

pub fn main() {
  game.till()
  game.plant(game.Carrot)
  io.println(
    "planted carrot at "
    <> int.to_string(game.get_pos_x())
    <> ","
    <> int.to_string(game.get_pos_y()),
  )
  let carrots = item.num_items(item.Carrot)
  io.println("carrots: " <> int.to_string(carrots))
}