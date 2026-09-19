// GleamFarmer utilities tour: every non-core game builtin in one file.
// Run headlessly: scripts/run-gleam.sh examples/utilities.gleam
// (the stub bridge returns canned values; in-game the real bridge runs these
// against the live farm, paced by op cost).

import game
import game/item
import gleam/bool
import gleam/float
import gleam/int
import gleam/io
import gleam/list
import gleam/option.{None, Some}

// Idiom: guard an action on a sensor. bool.guard returns `return` early when
// `when` is True, otherwise the block continues. Here we skip watering unless
// the tile is dry.
fn water_if_dry() {
  use <- bool.guard(when: game.get_water() >=. 0.5, return: Nil)
  case item.use_item(item.Water) {
    True -> io.println("watered")
    False -> io.println("out of water")
  }
}

// Idiom: check an action's Bool result with `case` instead of ignoring it.
fn try_plant() {
  case game.plant(game.Carrot) {
    True -> io.println("planted")
    False -> io.println("couldn't plant — till or wait")
  }
}

pub fn main() {
  // Movement + tile manipulation
  game.till()
  game.plant(game.Carrot)
  game.swap(game.East)
  // move the entity here to the tile to the East
  try_plant()

  // Sensors
  let pos = game.get_pos()
  io.println("pos: " <> int.to_string(pos.x) <> "," <> int.to_string(pos.y))
  io.println("world: " <> int.to_string(game.get_world_size()))
  io.println(
    "drones: "
    <> int.to_string(game.num_drones())
    <> "/"
    <> int.to_string(game.max_drones()),
  )
  case game.measure() {
    Some(progress) -> io.println("growth: " <> float.to_string(progress))
    None -> io.println("growth: none")
  }
  case game.get_companion() {
    Some(companion) ->
      io.println(
        "needs companion near: "
        <> int.to_string(companion.position.x)
        <> ","
        <> int.to_string(companion.position.y),
      )
    None -> io.println("no companion requirement")
  }

  // Costs + progression
  let cost = game.get_cost(game.Carrot)
  io.println(
    "carrot seed cost: " <> int.to_string(list.length(cost)) <> " item(s)",
  )
  io.println(
    "carrot unlocked: " <> int.to_string(game.num_unlocked(game.Carrot)),
  )
  game.unlock(game.Carrot)
  game.unlock_by_name("multi_trade")
  // capabilities use the _by_name escape hatch

  // Items
  water_if_dry()
  io.println("water: " <> int.to_string(item.num_items(item.Water)))
  item.use_items(item.Water, 2)
  item.use_item(item.Fertilizer)
  item.unlock_item(item.Hay)

  // World / pacing / cosmetics (safe, free print)
  game.set_world_size(5)
  game.set_execution_speed(1.0)
  game.do_a_flip()
  game.pet_the_piggy()
  game.change_hat("sombrero")
  echo "free, unpaced print"

  game.clear()
  // start over
}
