//! Game items (inventory).
//!
//! `import game/item` then `game.item.num_items(game.item.Water)`.

import gleam/option.{type Option, None, Some}

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

pub fn item_code(item: Item) -> Int {
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

fn item_name(item: Item) -> String {
  case item {
    Hay -> "hay"
    Wood -> "wood"
    Carrot -> "carrot"
    Pumpkin -> "pumpkin"
    Power -> "power"
    Gold -> "gold"
    Bones -> "bone"
    Water -> "water"
    Fertilizer -> "fertilizer"
  }
}

/// The `Item` for a game item id, or `None` for an unknown id.
pub fn item_from_id(id: Int) -> Option(Item) {
  case id {
    1 -> Some(Hay)
    2 -> Some(Wood)
    3 -> Some(Carrot)
    4 -> Some(Pumpkin)
    5 -> Some(Power)
    6 -> Some(Gold)
    7 -> Some(Bones)
    8 -> Some(Water)
    9 -> Some(Fertilizer)
    _ -> None
  }
}

@external(javascript, "../game_ffi.mjs", "num_items_code")
fn num_items_code(item: Int) -> Int

pub fn num_items(item: Item) -> Int {
  num_items_code(item_code(item))
}

@external(javascript, "../game_ffi.mjs", "use_item_code")
fn use_item_code(item: Int, count: Int) -> Bool

pub fn use_item(item: Item) -> Bool {
  use_item_code(item_code(item), 1)
}

/// Use `count` of an item at once (e.g. watering 3 units).
pub fn use_items(item: Item, count: Int) -> Bool {
  use_item_code(item_code(item), count)
}

@external(javascript, "../game_ffi.mjs", "unlock_code")
fn unlock_code(name: String) -> Bool

/// Spend resources to unlock or upgrade an item.
pub fn unlock_item(item: Item) -> Bool {
  unlock_code(item_name(item))
}

@external(javascript, "../game_ffi.mjs", "num_unlocked_code")
fn num_unlocked_code(name: String) -> Int

/// How many times an item's unlock has been bought (0 = not unlocked).
pub fn num_unlocked_item(item: Item) -> Int {
  num_unlocked_code(item_name(item))
}
