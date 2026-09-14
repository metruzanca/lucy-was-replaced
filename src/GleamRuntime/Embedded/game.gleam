//! GleamFarmer's bridge to The Farmer Was Replaced.
//!
//! `import game` gives typed access to the game world: the drone, the farm,
//! movement, planting, harvesting, and sensors. Actions are paced like the
//! game's own interpreter: each one costs ops and takes real time.

import gleam/option.{type Option, None, Some}

pub type Direction {
  North
  East
  South
  West
}

pub type Entity {
  Grass
  Bush
  Carrot
  Pumpkin
  Sunflower
  Tree
  Cactus
  Treasure
  Hedge
}

pub type Ground {
  Soil
  Grassland
}

@external(javascript, "./game_ffi.mjs", "move_code")
fn move_code(direction: Int) -> Bool

pub fn move(direction: Direction) -> Bool {
  case direction {
    North -> move_code(0)
    East -> move_code(1)
    South -> move_code(2)
    West -> move_code(3)
  }
}

@external(javascript, "./game_ffi.mjs", "can_move_code")
fn can_move_code(direction: Int) -> Bool

pub fn can_move(direction: Direction) -> Bool {
  case direction {
    North -> can_move_code(0)
    East -> can_move_code(1)
    South -> can_move_code(2)
    West -> can_move_code(3)
  }
}

@external(javascript, "./game_ffi.mjs", "harvest")
pub fn harvest() -> Bool

@external(javascript, "./game_ffi.mjs", "can_harvest")
pub fn can_harvest() -> Bool

fn entity_code(entity: Entity) -> Int {
  case entity {
    Grass -> 1
    Bush -> 2
    Carrot -> 3
    Pumpkin -> 4
    Sunflower -> 5
    Tree -> 6
    Cactus -> 7
    Treasure -> 8
    Hedge -> 9
  }
}

@external(javascript, "./game_ffi.mjs", "plant_code")
fn plant_code(entity: Int) -> Bool

pub fn plant(entity: Entity) -> Bool {
  plant_code(entity_code(entity))
}

@external(javascript, "./game_ffi.mjs", "till")
pub fn till() -> Nil

@external(javascript, "./game_ffi.mjs", "get_pos_x")
pub fn get_pos_x() -> Int

@external(javascript, "./game_ffi.mjs", "get_pos_y")
pub fn get_pos_y() -> Int

@external(javascript, "./game_ffi.mjs", "get_world_size")
pub fn get_world_size() -> Int

@external(javascript, "./game_ffi.mjs", "get_entity_type_code")
fn get_entity_type_code() -> Int

pub fn get_entity_type() -> Option(Entity) {
  case get_entity_type_code() {
    0 -> None
    1 -> Some(Grass)
    2 -> Some(Bush)
    3 -> Some(Carrot)
    4 -> Some(Pumpkin)
    5 -> Some(Sunflower)
    6 -> Some(Tree)
    7 -> Some(Cactus)
    8 -> Some(Treasure)
    9 -> Some(Hedge)
    _ -> None
  }
}

@external(javascript, "./game_ffi.mjs", "get_ground_type_code")
fn get_ground_type_code() -> Int

pub fn get_ground_type() -> Ground {
  case get_ground_type_code() {
    1 -> Soil
    _ -> Grassland
  }
}

@external(javascript, "./game_ffi.mjs", "get_water")
pub fn get_water() -> Float

@external(javascript, "./game_ffi.mjs", "get_time")
pub fn get_time() -> Float

@external(javascript, "./game_ffi.mjs", "get_tick_count")
pub fn get_tick_count() -> Int