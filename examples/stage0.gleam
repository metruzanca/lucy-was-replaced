import game
import gleam/io
import gleam/option.{None, Some}

pub fn main() {
  let name = "my first farm"
  io.println("hello, " <> name)

  case game.get_entity_type() {
    None -> io.println("bare ground")
    Some(game.Grass) -> io.println("grass under me")
    Some(_) -> io.println("something else")
  }
}