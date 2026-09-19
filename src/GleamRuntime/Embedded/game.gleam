//! GleamFarmer's bridge to The Farmer Was Replaced.
//!
//! `import game` gives typed access to the game world: the drone, the farm,
//! movement, planting, harvesting, sensors, and utilities (swap, clear,
//! measure, costs, progression, cosmetics). Actions are paced like the game's
//! own interpreter: each one costs ops and takes real time.

import game/item
import gleam/dynamic.{classify, type Dynamic}
import gleam/dynamic/decode
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

/// A position on the farm grid.
pub type Position {
  Position(x: Int, y: Int)
}

/// The value `measure()` returns. A `MeasureValue` is an entity-specific number
/// (sunflower petals, cactus size, pumpkin number); a `MeasurePosition` is the
/// position of a treasure (in a maze) or an apple's next position.
pub type Measure {
  MeasureValue(value: Int)
  MeasurePosition(position: Position)
}

/// The companion a growable needs nearby.
pub type Companion {
  Companion(entity: Entity, position: Position)
}

fn direction_code(direction: Direction) -> Int {
  case direction {
    North -> 0
    East -> 1
    South -> 2
    West -> 3
  }
}

@external(javascript, "./game_ffi.mjs", "move_code")
fn move_code(direction: Int) -> Bool

pub fn move(direction: Direction) -> Bool {
  move_code(direction_code(direction))
}

@external(javascript, "./game_ffi.mjs", "can_move_code")
fn can_move_code(direction: Int) -> Bool

pub fn can_move(direction: Direction) -> Bool {
  can_move_code(direction_code(direction))
}

@external(javascript, "./game_ffi.mjs", "harvest")
pub fn harvest() -> Bool

@external(javascript, "./game_ffi.mjs", "can_harvest")
pub fn can_harvest() -> Bool

pub fn entity_code(entity: Entity) -> Int {
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

fn entity_name(entity: Entity) -> String {
  case entity {
    Grass -> "grass"
    Bush -> "bush"
    Carrot -> "carrot"
    Pumpkin -> "pumpkin"
    Sunflower -> "sunflower"
    Tree -> "tree"
    Cactus -> "cactus"
    Treasure -> "treasure"
    Hedge -> "hedge"
  }
}

fn entity_from_code(code: Int) -> Entity {
  case code {
    1 -> Grass
    2 -> Bush
    3 -> Carrot
    4 -> Pumpkin
    5 -> Sunflower
    6 -> Tree
    7 -> Cactus
    8 -> Treasure
    9 -> Hedge
    _ -> Grass
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
fn get_pos_x() -> Int

@external(javascript, "./game_ffi.mjs", "get_pos_y")
fn get_pos_y() -> Int

/// The drone's current position on the grid.
pub fn get_pos() -> Position {
  Position(x: get_pos_x(), y: get_pos_y())
}

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

// ---- utilities ----

@external(javascript, "./game_ffi.mjs", "swap_code")
fn swap_code(direction: Int) -> Bool

/// Move the entity on the current tile to the adjacent tile (if it is empty).
pub fn swap(direction: Direction) -> Bool {
  swap_code(direction_code(direction))
}

/// Clear the whole farm: despawns extra drones, resets the drone, and removes
/// every entity and ground.
@external(javascript, "./game_ffi.mjs", "clear")
pub fn clear() -> Nil

@external(javascript, "./game_ffi.mjs", "measure")
fn measure_code() -> Dynamic

/// Measures the entity on the current tile. The value depends on the entity:
/// `MeasureValue` for a sunflower's petal count, a cactus's size, or a
/// pumpkin's number; `MeasurePosition` for a treasure's position.
/// Returns `None` when the tile is empty or the entity cannot be measured.
pub fn measure() -> Option(Measure) {
  case decode.run(measure_code(), decode.list(of: decode.int)) {
    Ok([value]) -> Some(MeasureValue(value: value))
    Ok([x, y]) -> Some(MeasurePosition(position: Position(x: x, y: y)))
    _ -> None
  }
}

@external(javascript, "./game_ffi.mjs", "measure_at_code")
fn measure_at_code(direction: Int) -> Dynamic

/// Measures the entity on the adjacent tile in `direction`.
pub fn measure_at(direction: Direction) -> Option(Measure) {
  case decode.run(measure_at_code(direction_code(direction)), decode.list(of: decode.int)) {
    Ok([value]) -> Some(MeasureValue(value: value))
    Ok([x, y]) -> Some(MeasurePosition(position: Position(x: x, y: y)))
    _ -> None
  }
}

@external(javascript, "./game_ffi.mjs", "get_companion")
fn companion_code() -> Dynamic

/// The companion a growable needs nearby: `Some(Companion)`, or `None` when
/// the entity has no companion requirement.
pub fn get_companion() -> Option(Companion) {
  case decode.run(companion_code(), decode.list(of: decode.int)) {
    Ok([code, x, y]) ->
      Some(Companion(
        entity: entity_from_code(code),
        position: Position(x: x, y: y),
      ))
    _ -> None
  }
}

@external(javascript, "./game_ffi.mjs", "get_cost_code")
fn cost_code(entity: Int) -> Dynamic

/// The seed (or item) cost of growing an entity, as item counts.
pub fn get_cost(entity: Entity) -> List(#(item.Item, Int)) {
  case decode.run(cost_code(entity_code(entity)), decode.list(of: decode.int)) {
    Ok(values) -> cost_pairs(values)
    Error(_) -> []
  }
}

fn cost_pairs(values: List(Int)) -> List(#(item.Item, Int)) {
  case values {
    [id, count, ..rest] -> {
      case item.item_from_id(id) {
        Some(it) -> [#(it, count), ..cost_pairs(rest)]
        None -> cost_pairs(rest)
      }
    }
    _ -> []
  }
}

@external(javascript, "./game_ffi.mjs", "random")
pub fn random() -> Float

@external(javascript, "./game_ffi.mjs", "num_drones")
pub fn num_drones() -> Int

@external(javascript, "./game_ffi.mjs", "max_drones")
pub fn max_drones() -> Int

@external(javascript, "./game_ffi.mjs", "unlock_code")
fn unlock_code(name: String) -> Bool

/// Spend resources to unlock (or upgrade) an entity, e.g. its seeds.
pub fn unlock(entity: Entity) -> Bool {
  unlock_code(entity_name(entity))
}

/// Spend resources to unlock anything by its unlock name (e.g. "multi_trade").
pub fn unlock_by_name(name: String) -> Bool {
  unlock_code(name)
}

@external(javascript, "./game_ffi.mjs", "num_unlocked_code")
fn num_unlocked_code(name: String) -> Int

/// How many times an entity's unlock has been bought (0 = not unlocked).
pub fn num_unlocked(entity: Entity) -> Int {
  num_unlocked_code(entity_name(entity))
}

/// How many times an unlock name has been bought (0 = not unlocked).
pub fn num_unlocked_by_name(name: String) -> Int {
  num_unlocked_code(name)
}

@external(javascript, "./game_ffi.mjs", "set_execution_speed")
pub fn set_execution_speed(speed: Float) -> Nil

@external(javascript, "./game_ffi.mjs", "set_world_size")
pub fn set_world_size(size: Int) -> Nil

@external(javascript, "./game_ffi.mjs", "do_a_flip")
pub fn do_a_flip() -> Nil

@external(javascript, "./game_ffi.mjs", "pet_the_piggy")
pub fn pet_the_piggy() -> Nil

@external(javascript, "./game_ffi.mjs", "change_hat")
pub fn change_hat(name: String) -> Nil

/// Print without pacing (free, no op cost) — unlike `io.println`.
@external(javascript, "./game_ffi.mjs", "quick_print")
pub fn quick_print(text: String) -> Nil

// ---- drones ----

/// A handle to a spawned drone, for `wait_for` / `has_finished`.
pub type DroneHandle {
  DroneHandle(id: Int, generation: Int)
}

@external(javascript, "./game_ffi.mjs", "spawn_drone_code")
fn spawn_drone_code(count: Int, worker: fn() -> Dynamic) -> Dynamic

@external(javascript, "./game_ffi.mjs", "spawn_drone_code")
fn spawn_drone_code_nil(count: Int, worker: fn() -> Nil) -> Dynamic

/// Spawn `count` drones running `worker`. The worker must be a named `pub fn`
/// in your module (spawned drones run it in their own engine). Each drone can
/// tell itself apart with `get_drone_id()`.
pub fn spawn_drone(count: Int, worker: fn() -> Nil) -> List(DroneHandle) {
  spawn_drone_code_nil(count, worker)
  |> decode_handles
}

/// Like `spawn_drone`, but the worker's return value is available via `wait_for`.
pub fn spawn_drone_with(count: Int, worker: fn() -> Dynamic) -> List(DroneHandle) {
  spawn_drone_code(count, worker)
  |> decode_handles
}

fn decode_handles(value: Dynamic) -> List(DroneHandle) {
  case decode.run(value, decode.list(of: decode.int)) {
    Ok(flat) -> handles_from_flat(flat)
    Error(_) -> []
  }
}

fn handles_from_flat(values: List(Int)) -> List(DroneHandle) {
  case values {
    [id, generation, ..rest] ->
      [DroneHandle(id: id, generation: generation), ..handles_from_flat(rest)]
    _ -> []
  }
}

/// This drone's id (0 is the main drone).
@external(javascript, "./game_ffi.mjs", "get_drone_id")
pub fn get_drone_id() -> Int

@external(javascript, "./game_ffi.mjs", "wait_for_code")
fn wait_for_code(id: Int, generation: Int) -> Dynamic

/// Block until the drone finishes; returns its result as a `Dynamic`
/// (decode it with `gleam/dynamic/decode`), or `None` if it cannot be awaited.
pub fn wait_for(handle: DroneHandle) -> Option(Dynamic) {
  let value = wait_for_code(handle.id, handle.generation)
  case classify(value) {
    "Nil" -> None
    _ -> Some(value)
  }
}

/// Whether the drone has finished (non-blocking).
@external(javascript, "./game_ffi.mjs", "has_finished_code")
fn has_finished_code(id: Int, generation: Int) -> Bool

pub fn has_finished(handle: DroneHandle) -> Bool {
  has_finished_code(handle.id, handle.generation)
}

@external(javascript, "./game_ffi.mjs", "send_code")
fn send_code(message: Dynamic, to_drone_id: Int) -> Nil

/// Send a message (any JSON-serialisable value) to another drone's mailbox.
pub fn send(message: Dynamic, to_drone_id: Int) -> Nil {
  send_code(message, to_drone_id)
}

@external(javascript, "./game_ffi.mjs", "receive_code")
fn receive_code(from_drone_id: Int) -> Dynamic

/// Receive a message from any drone (non-blocking); `None` if the mailbox is empty.
pub fn receive() -> Option(Dynamic) {
  let value = receive_code(-1)
  case classify(value) {
    "Nil" -> None
    _ -> Some(value)
  }
}

/// Receive a message sent specifically by the given drone id.
pub fn receive_from(drone_id: Int) -> Option(Dynamic) {
  let value = receive_code(drone_id)
  case classify(value) {
    "Nil" -> None
    _ -> Some(value)
  }
}
