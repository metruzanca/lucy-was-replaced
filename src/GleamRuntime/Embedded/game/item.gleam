//! Game items (inventory).
//!
//! `import game/item` then `game.item.num_items(game.item.Water)`.

pub type Item {
  Hay
  Wood
  Carrot
  Pumpkin
  Power
  Gold
  Bones
  Water
  Fertilizer
}

fn item_code(item: Item) -> Int {
  case item {
    Hay -> 1
    Wood -> 2
    Carrot -> 3
    Pumpkin -> 4
    Power -> 5
    Gold -> 6
    Bones -> 7
    Water -> 8
    Fertilizer -> 9
  }
}

@external(javascript, "../game_ffi.mjs", "num_items_code")
fn num_items_code(item: Int) -> Int

pub fn num_items(item: Item) -> Int {
  num_items_code(item_code(item))
}

@external(javascript, "../game_ffi.mjs", "use_item_code")
fn use_item_code(item: Int) -> Bool

pub fn use_item(item: Item) -> Bool {
  use_item_code(item_code(item))
}