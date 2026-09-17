import game
import gleam/io
import gleam/int

pub fn main() {
  let start = game.get_tick_count()
  game.till()
  game.plant(game.Carrot)
  io.println(
    "ticks used: " <> int.to_string(game.get_tick_count() - start),
  )
}