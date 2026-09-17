import game
import game/item
import gleam/int
import gleam/io
import gleam/option.{None, Some}

pub fn main() {
  game.till()
  io.println("ground: " <> ground_name(game.get_ground_type()))

  let planted = game.plant(game.Carrot)
  io.println("plant returned: " <> bool_str(planted))

  case game.get_entity_type() {
    Some(game.Carrot) -> io.println("entity: Carrot OK")
    Some(other) -> io.println("entity: " <> entity_name(other))
    None -> io.println("entity: none FAIL")
  }

  io.println("carrots: " <> int.to_string(item.num_items(item.Carrot)))
}

fn bool_str(b: Bool) -> String {
  case b {
    True -> "true"
    False -> "false"
  }
}

fn ground_name(g: game.Ground) -> String {
  case g {
    game.Soil -> "soil"
    game.Grassland -> "grassland"
  }
}

fn entity_name(e: game.Entity) -> String {
  case e {
    game.Grass -> "grass"
    game.Bush -> "bush"
    game.Carrot -> "carrot"
    game.Pumpkin -> "pumpkin"
    game.Sunflower -> "sunflower"
    game.Tree -> "tree"
    game.Cactus -> "cactus"
    game.Treasure -> "treasure"
    game.Hedge -> "hedge"
  }
}
