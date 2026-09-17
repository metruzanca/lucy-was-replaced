import game
import gleam/io
import gleam/int

pub fn main() {
  plant_row(3)
}

fn plant_row(n: Int) -> Nil {
  case n {
    0 -> Nil
    _ -> {
      game.till()
      game.plant(game.Carrot)
      let pos = game.get_pos()
      io.println("planted at " <> int.to_string(pos.x))
      game.move(game.East)
      plant_row(n - 1)
    }
  }
}