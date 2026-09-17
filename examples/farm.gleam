import game
import game/item
import gleam/int
import gleam/io

pub fn main() {
  game.till()
  game.plant(game.Carrot)
  let pos = game.get_pos()
  io.println(
    "planted carrot at " <> int.to_string(pos.x) <> "," <> int.to_string(pos.y),
  )
  let carrots = item.num_items(item.Carrot)
  io.println("carrots: " <> int.to_string(carrots))
}
