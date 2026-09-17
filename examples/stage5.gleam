import game
import game/item
import gleam/io

pub type Crop {
  Crop(name: String, entity: game.Entity, item: item.Item)
}

pub fn main() {
  let carrot = Crop("carrot", game.Carrot, item.Carrot)
  let fancy = Crop(..carrot, name: "heirloom carrot")
  io.println(fancy.name)
}