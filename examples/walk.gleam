import game
import game/item
import gleam/int
import gleam/io

fn print_pos(label: String) {
  let pos = game.get_pos()
  io.println(label <> int.to_string(pos.x) <> "," <> int.to_string(pos.y))
}

pub fn main() {
  io.println("farm size: " <> int.to_string(game.get_world_size()))
  game.till()
  game.plant(game.Carrot)
  print_pos("planted at ")
  game.move(game.East)
  print_pos("east -> ")
  game.move(game.South)
  print_pos("south -> ")
  game.move(game.West)
  print_pos("west -> ")
  game.move(game.North)
  print_pos("home at ")
}
