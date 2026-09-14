import game
import game/item
import gleam/io
import gleam/int

pub fn main() {
  io.println("farm size: " <> int.to_string(game.get_world_size()))
  game.till()
  game.plant(game.Carrot)
  io.println("planted at 0,0")
  game.move(game.East)
  io.println("east -> " <> int.to_string(game.get_pos_x()))
  game.move(game.South)
  io.println("south -> " <> int.to_string(game.get_pos_y()))
  game.move(game.West)
  io.println("west -> " <> int.to_string(game.get_pos_x()))
  game.move(game.North)
  io.println("home at " <> int.to_string(game.get_pos_x()) <> "," <> int.to_string(game.get_pos_y()))
}