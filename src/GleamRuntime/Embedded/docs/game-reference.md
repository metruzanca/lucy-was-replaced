# GleamFarmer — the `game` library

The in-game reference for *Lucy was Replaced*. Every open code window is an
importable Gleam module; `import game` gives you the farm. This file is the
single source of truth for the in-game docs panel — it is split on `## `
headings into the game's doc pages.

## game module

`import game` gives typed access to the world: the drone, the farm, movement,
planting, harvesting, sensors, and utilities (swap, clear, measure, costs,
progression, cosmetics). Items live in a second module: `import game/item`,
then `item.num_items(item.Hay)`.

Every action is paced like the game's own interpreter: it costs ops and takes
real time (speed upgrades scale it). Sensors return instantly. `get_time()`
returns op-accounted execution time.

Logging:

- `io.println("...")` prints like the game's `print`: paced, with a bubble above
  the drone and an op cost. Use `import gleam/io` first.
- `echo value` prints instantly to the log — free, no op cost, no bubble (like
  `game.quick_print`). `echo` is a Gleam keyword: it prints any value and
  evaluates to that value. Output is prefixed with `file:line`.

Custom types:

- `game.Direction` — `North` / `East` / `South` / `West`
- `game.Entity` — `Grass`, `Bush`, `Carrot`, `Pumpkin`, `Sunflower`, `Tree`, `Cactus`, `Treasure`, `Hedge`
- `game.Ground` — `Soil` / `Grassland`
- `game.Position` — `Position(x: Int, y: Int)`
- `game.Measure` — `MeasureValue(value: Int)` / `MeasurePosition(position: game.Position)`
- `game.Companion` — `Companion(entity: game.Entity, position: game.Position)`
- `game.item.Item` — `Hay`, `Wood`, `Carrot`, `Pumpkin`, `Power`, `Gold`, `Bones`, `Water`, `Fertilizer`
- `game.DroneHandle` — for `wait_for` / `has_finished`

A first program, split across two windows — any open code window is an
importable Gleam module:

`utils.gleam`:

```
import game
import gleam/list

pub fn visit_all(size: Int, do_work) {
  use _ <- list.each(list.repeat(True, size))
  game.move(game.East)
  use _ <- list.each(list.repeat(True, size))
  game.move(game.North)
  do_work()
}
```

`main.gleam`:

```
import game
import utils

pub fn main() {
  utils.visit_all(3, fn() {
    game.till()
    game.plant(game.Carrot)
  })
}
```

### Python builtins → Gleam stdlib

The game's Python has builtins that Gleam does not. Use the Gleam stdlib
(`gleam/list`, `gleam/string`, `gleam/set`, `gleam/dict`, `gleam/int`,
`gleam/float`) instead:

- `range(n)` → `gleam/list.range(0, n)`
- `len(x)` → `gleam/list.length(x)` (or `gleam/string.length`)
- `min` / `max` → fold over a list with `gleam/int.min` / `gleam/int.max`
- `abs(n)` → `gleam/int.abs` / `gleam/float.abs`
- `str(x)` → `gleam/string.inspect(x)`
- `list` / `set` / `dict` → the `gleam/list`, `gleam/set`, `gleam/dict` modules
- `print(...)` → `gleam/io.println` (paced); for free output use `echo value`

## game.harvest

`game.harvest() -> Bool`

Harvests the entity under the drone. If you harvest an entity that can't be
harvested, it is destroyed.

returns `True` if an entity was removed, `False` otherwise.

takes `200` ticks to execute if an entity was removed, `1` tick otherwise.

example:

```
game.harvest()
```

## game.can_harvest

`game.can_harvest() -> Bool`

Used to find out if plants are fully grown.

returns `True` if there is an entity under the drone that is ready to be
harvested, `False` otherwise.

takes `1` tick to execute.

example:

```
case game.can_harvest() {
  True -> {
    let _ = game.harvest()
    Nil
  }
  False -> Nil
}
```

## game.plant

`game.plant(entity: game.Entity) -> Bool`

Spends the cost of `entity` and plants it under the drone. It fails if you
can't afford the plant, the ground type is wrong, or there's already a plant
there.

returns `True` if it succeeded, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
game.plant(game.Carrot)
```

## game.move

`game.move(direction: game.Direction) -> Bool`

Moves the drone in `direction` by one tile. Moving over the edge of the farm
wraps to the other side.

- `game.North` = up
- `game.East` = right
- `game.South` = down
- `game.West` = left

returns `True` if the drone moved, `False` otherwise.

takes `200` ticks to execute if the drone moved, `1` tick otherwise.

example:

```
game.move(game.North)
```

## game.can_move

`game.can_move(direction: game.Direction) -> Bool`

Checks if the drone can move in `direction`.

returns `True` if the drone can move, `False` otherwise.

takes `1` tick to execute.

example:

```
case game.can_move(game.North) {
  True -> {
    let _ = game.move(game.North)
    Nil
  }
  False -> Nil
}
```

## game.till

`game.till() -> Nil`

Tills the ground under the drone into `game.Soil`. If it is already soil it
changes the ground back to `game.Grassland`.

takes `200` ticks to execute.

example:

```
game.till()
```

## game.swap

`game.swap(direction: game.Direction) -> Bool`

Swaps the entity under the drone with the entity next to it in `direction`.
Doesn't work on all entities. Also works if one (or both) of the tiles are
empty.

returns `True` if it succeeded, `False` otherwise.

takes `200` ticks to execute on success, `1` tick otherwise.

example:

```
game.swap(game.East)
```

## game.clear

`game.clear() -> Nil`

Removes everything from the farm, moves the drone back to `(0, 0)` and changes
the hat back to the straw hat.

takes `200` ticks to execute.

example:

```
game.clear()
```

## game.get_pos

`game.get_pos() -> game.Position`

The drone's current position on the grid. `x` starts at `0` in the west and
increases east; `y` starts at `0` in the south and increases north.

takes `1` tick to execute.

example:

```
let pos = game.get_pos()
echo "x: " <> int.to_string(pos.x)
```

## game.get_world_size

`game.get_world_size() -> Int`

The current side length of the farm grid.

takes `1` tick to execute.

example:

```
let size = game.get_world_size()
```

## game.get_entity_type

`game.get_entity_type() -> Option(game.Entity)`

The type of the entity under the drone, or `None` if the tile is empty.

takes `1` tick to execute.

example:

```
case game.get_entity_type() {
  Some(game.Grass) -> {
    let _ = game.harvest()
    Nil
  }
  _ -> Nil
}
```

## game.get_ground_type

`game.get_ground_type() -> game.Ground`

The type of the ground under the drone.

takes `1` tick to execute.

example:

```
case game.get_ground_type() {
  game.Soil -> Nil
  _ -> game.till()
}
```

## game.get_water

`game.get_water() -> Float`

The current water level under the drone, between `0` and `1`.

takes `1` tick to execute.

example:

```
case game.get_water() <=. 0.5 {
  True -> {
    let _ = item.use_item(item.Water)
    Nil
  }
  False -> Nil
}
```

## game.get_time

`game.get_time() -> Float`

Op-accounted execution time in seconds — how long the current run has been
executing (`ops × OpDuration`), like the game's own interpreter. The world
clock is unchanged.

takes `0` ticks to execute.

example:

```
let start = game.get_time()
```

## game.get_tick_count

`game.get_tick_count() -> Int`

The number of ticks performed since the start of execution (ops merged across
pure computation and actions).

takes `0` ticks to execute.

example:

```
echo int.to_string(game.get_tick_count())
```

## game.measure

`game.measure() -> Option(game.Measure)`

Measures the entity on the current tile. The value depends on the entity:

- `Some(game.MeasureValue(n))` — a number: a sunflower's petal count, a
  cactus's size, or a pumpkin's mysterious number.
- `Some(game.MeasurePosition(position))` — the position of a treasure (the
  gold is hidden at that position in the maze).
- `None` — the tile is empty or the entity cannot be measured.

takes `1` tick to execute.

example:

```
case game.measure() {
  Some(game.MeasureValue(petals)) -> {
    let _ = echo "sunflower with " <> int.to_string(petals) <> " petals"
    Nil
  }
  Some(game.MeasurePosition(position)) -> {
    let _ = echo "treasure at " <> int.to_string(position.x)
    Nil
  }
  None -> Nil
}
```

## game.measure_at

`game.measure_at(direction: game.Direction) -> Option(game.Measure)`

Measures the entity on the adjacent tile in `direction`, with the same values
as `game.measure()`.

takes `1` tick to execute.

example:

```
case game.measure_at(game.North) {
  Some(game.MeasureValue(petals)) -> {
    let _ = echo "north has " <> int.to_string(petals) <> " petals"
    Nil
  }
  Some(game.MeasurePosition(position)) -> {
    let _ = echo "treasure at " <> int.to_string(position.x)
    Nil
  }
  None -> Nil
}
```

## game.get_companion

`game.get_companion() -> Option(game.Companion)`

The companion the growable under the drone needs nearby:
`Some(Companion(entity, Position(x, y)))`, or `None` when it has no companion
requirement.

takes `1` tick to execute.

example:

```
case game.get_companion() {
  Some(_) -> {
    let _ = echo "needs a companion"
    Nil
  }
  None -> Nil
}
```

## game.get_cost

`game.get_cost(entity: game.Entity) -> List(#(item.Item, Int))`

The seed (or item) cost of growing `entity`, as `(item, count)` pairs.

takes `1` tick to execute.

example:

```
let cost = game.get_cost(game.Carrot)
```

## game.random

`game.random() -> Float`

A random number between `0` (inclusive) and `1` (exclusive).

takes `1` tick to execute.

example:

```
let roll = game.random()
```

## game.item.num_items

`game.item.num_items(item: game.item.Item) -> Int`

How much of `item` you currently have.

takes `1` tick to execute.

example:

```
case item.num_items(item.Fertilizer) > 0 {
  True -> {
    let _ = item.use_item(item.Fertilizer)
    Nil
  }
  False -> Nil
}
```

## game.item.use_item

`game.item.use_item(item: game.item.Item) -> Bool`

Attempts to use `item` once (e.g. watering one unit). Works with items like
`item.Water` and `item.Fertilizer`. For a larger amount at once use
`item.use_items(item, count)`.

returns `True` if an item was used, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
let _ = item.use_item(item.Water)
```

## game.item.use_items

`game.item.use_items(item: game.item.Item, count: Int) -> Bool`

Attempts to use `count` of `item` at once (e.g. watering `3` units).

returns `True` if an item was used, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
let _ = item.use_items(item.Water, 3)
```

## game.item.unlock_item

`game.item.unlock_item(item: game.item.Item) -> Bool`

Spend resources to unlock or upgrade `item` (e.g. its seeds).

returns `True` if the unlock was successful, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
let _ = item.unlock_item(item.Carrot)
```

## game.item.num_unlocked_item

`game.item.num_unlocked_item(item: game.item.Item) -> Int`

How many times an item's unlock has been bought (`0` = not unlocked).

takes `1` tick to execute.

example:

```
let level = item.num_unlocked_item(item.Carrot)
```

## game.unlock

`game.unlock(entity: game.Entity) -> Bool`

Spend resources to unlock (or upgrade) an entity, e.g. its seeds. Has exactly
the same effect as clicking the button in the research tree.

returns `True` if the unlock was successful, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
let _ = game.unlock(game.Carrot)
```

## game.unlock_by_name

`game.unlock_by_name(name: String) -> Bool`

Spend resources to unlock anything by its unlock name (e.g. `"multi_trade"`),
when there is no `game` type for it.

returns `True` if the unlock was successful, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
let _ = game.unlock_by_name("multi_trade")
```

## game.num_unlocked

`game.num_unlocked(entity: game.Entity) -> Int`

How many times an entity's unlock has been bought (`0` = not unlocked). For
upgradeable unlocks this is `1` plus the number of upgrades.

takes `1` tick to execute.

example:

```
let level = game.num_unlocked(game.Bush)
```

## game.num_unlocked_by_name

`game.num_unlocked_by_name(name: String) -> Int`

How many times an unlock name has been bought (`0` = not unlocked).

takes `1` tick to execute.

example:

```
let level = game.num_unlocked_by_name("mazes")
```

## game.set_execution_speed

`game.set_execution_speed(speed: Float) -> Nil`

Limits the speed at which the program is executed to better see what's
happening. `1` is the speed without upgrades; `10` makes the code execute `10`
times faster (the speed of the drone after `9` speed upgrades); `0.5` runs at
half speed. A faster-than-maximum speed just runs at max. `0` or negative
resets to max speed. The effect stops when the execution stops.

takes `200` ticks to execute.

example:

```
game.set_execution_speed(1.0)
```

## game.set_world_size

`game.set_world_size(size: Int) -> Nil`

Limits the farm to a `size` x `size` grid, clears the farm and resets the
drone position. The smallest size is `3`; smaller resets to full size. The
effect stops when the execution stops.

takes `200` ticks to execute.

example:

```
game.set_world_size(5)
```

## game.do_a_flip

`game.do_a_flip() -> Nil`

Makes the drone do a flip! Not affected by speed upgrades.

takes `1s` to execute.

example:

```
game.do_a_flip()
```

## game.pet_the_piggy

`game.pet_the_piggy() -> Nil`

Pets the piggy! Not affected by speed upgrades.

takes `1s` to execute.

example:

```
game.pet_the_piggy()
```

## game.change_hat

`game.change_hat(name: String) -> Nil`

Changes the hat of the drone to `name` (e.g. `"dinosaur"`).

takes `200` ticks to execute.

example:

```
game.change_hat("dinosaur")
```

## game.quick_print

`game.quick_print(text: String) -> Nil`

Prints `text` to the output page without stopping to write it into the air —
free, no op cost (unlike `gleam/io.println`).

Gleam's `echo value` keyword does the same: free, unpaced output for any value
(with a `file:line` prefix).

takes `0` ticks to execute.

example:

```
game.quick_print("hi mom")
```

## game.num_drones

`game.num_drones() -> Int`

The number of drones currently in the farm.

takes `1` tick to execute.

example:

```
game.quick_print(int.to_string(game.num_drones()))
```

## game.max_drones

`game.max_drones() -> Int`

The maximum number of drones you can have in the farm.

takes `1` tick to execute.

example:

```
let limit = game.max_drones()
```

## game.spawn_drone

`game.spawn_drone(count: Int, worker: fn() -> Nil) -> List(game.DroneHandle)`

Spawns `count` drones that run `worker` in their own engine, concurrently.
`worker` must be a named `pub fn` in your module (spawned drones run it by
name). Each drone can tell itself apart with `game.get_drone_id()`.

returns a list of `game.DroneHandle`s for `game.wait_for` / `game.has_finished`.

takes `200` ticks to execute if a drone was spawned, `1` tick otherwise.

example:

```
pub fn worker() -> Nil {
  let me = game.get_drone_id()
  // farm this drone's quadrant...
  Nil
}

pub fn main() {
  let handles = game.spawn_drone(3, worker)
  let _ = handles
}
```

## game.spawn_drone_with

`game.spawn_drone_with(count: Int, worker: fn() -> Dynamic) -> List(game.DroneHandle)`

Like `game.spawn_drone`, but the worker's return value is available via
`game.wait_for`.

takes `200` ticks to execute if a drone was spawned, `1` tick otherwise.

example:

```
pub fn worker() -> Dynamic {
  dynamic.bool(game.harvest())
}

pub fn main() {
  let handles = game.spawn_drone_with(1, worker)
  case handles {
    [handle, ..] -> {
      let _ = game.wait_for(handle)
      Nil
    }
    _ -> Nil
  }
}
```

## game.get_drone_id

`game.get_drone_id() -> Int`

This drone's id (`0` is the main drone).

takes `0` ticks to execute.

example:

```
let me = game.get_drone_id()
```

## game.wait_for

`game.wait_for(handle: game.DroneHandle) -> Option(Dynamic)`

Blocks until the drone finishes, returning its result as a `Dynamic` (decode
it with `gleam/dynamic/decode`), or `None` if it cannot be awaited.

takes `1` tick to execute if the awaited drone is already done.

example:

```
case game.wait_for(handle) {
  Some(_) -> {
    let _ = echo "done"
    Nil
  }
  None -> Nil
}
```

## game.has_finished

`game.has_finished(handle: game.DroneHandle) -> Bool`

Checks (non-blocking) if the drone has finished.

takes `1` tick to execute.

example:

```
case game.has_finished(handle) {
  True -> {
    let _ = game.wait_for(handle)
    Nil
  }
  False -> Nil
}
```

## game.send

`game.send(message: Dynamic, to_drone_id: Int) -> Nil`

Sends a message (any JSON-serialisable value) to another drone's mailbox.

takes `0` ticks to execute.

example:

```
game.send("hello", 1)
```

## game.receive

`game.receive() -> Option(Dynamic)`

Receives a message from any drone (non-blocking); `None` if the mailbox is
empty.

takes `0` ticks to execute.

example:

```
case game.receive() {
  Some(_) -> {
    let _ = echo "got something"
    Nil
  }
  None -> Nil
}
```

## game.receive_from

`game.receive_from(drone_id: Int) -> Option(Dynamic)`

Receives a message sent specifically by the given drone id (non-blocking).

takes `0` ticks to execute.

example:

```
case game.receive_from(0) {
  Some(_) -> {
    let _ = echo "got something"
    Nil
  }
  None -> Nil
}
```

## game.North

`game.North : game.Direction`

The up direction on the screen. Unless you turn your screen around.

example:

```
game.move(game.North)
```

## game.East

`game.East : game.Direction`

The right direction on the screen. Unless you turn your screen around.

example:

```
game.move(game.East)
```

## game.South

`game.South : game.Direction`

The down direction on the screen. Unless you turn your screen around.

example:

```
game.move(game.South)
```

## game.West

`game.West : game.Direction`

The left direction on the screen. Unless you turn your screen around.

example:

```
game.move(game.West)
```

## game.Grass

`game.Grass : game.Entity`

Grows automatically on grassland. Harvest it to obtain `game.item.Hay`.

example:

```
game.plant(game.Grass)
```

## game.Bush

`game.Bush : game.Entity`

A small bush that drops `game.item.Wood`.

example:

```
game.plant(game.Bush)
```

## game.Tree

`game.Tree : game.Entity`

Trees drop more wood than bushes. They take longer to grow if other trees grow
next to them.

example:

```
game.plant(game.Tree)
```

## game.Carrot

`game.Carrot : game.Entity`

Carrots!

example:

```
game.plant(game.Carrot)
```

## game.Pumpkin

`game.Pumpkin : game.Entity`

Pumpkins grow together when they are next to other fully grown pumpkins.
About 1 in 5 pumpkins dies when it grows up.

example:

```
game.plant(game.Pumpkin)
```

## game.Sunflower

`game.Sunflower : game.Entity`

Sunflowers collect the power from the sun. Harvesting them gives you
`game.item.Power`. Harvesting a sunflower with the maximum number of petals
gives bonus power.

example:

```
game.plant(game.Sunflower)
```

## game.Cactus

`game.Cactus : game.Entity`

Cacti come in 10 different sizes. When harvested, adjacent cacti that are
"sorted" are also harvested.

example:

```
game.plant(game.Cactus)
```

## game.Hedge

`game.Hedge : game.Entity`

Part of the maze.

example:

```
game.plant(game.Hedge)
```

## game.Treasure

`game.Treasure : game.Entity`

A treasure that contains gold equal to the side length of the maze in which it
is hidden. It can be harvested like a plant.

example:

```
game.plant(game.Treasure)
```

## game.Soil

`game.Soil : game.Ground`

Calling `game.till()` turns the ground into soil. Calling it again changes it
back to `game.Grassland`. Most crops grow on soil.

example:

```
case game.get_ground_type() {
  game.Soil -> Nil
  _ -> game.till()
}
```

## game.Grassland

`game.Grassland : game.Ground`

The default ground. Grass will automatically grow on it.

example:

```
case game.get_ground_type() {
  game.Grassland -> Nil
  _ -> game.till()
}
```

## game.item.Hay

`game.item.Hay : game.item.Item`

Gathered by harvesting grass.

example:

```
let n = item.num_items(item.Hay)
```

## game.item.Wood

`game.item.Wood : game.item.Item`

Gathered by harvesting bushes and trees.

example:

```
let n = item.num_items(item.Wood)
```

## game.item.Carrot

`game.item.Carrot : game.item.Item`

Gathered by harvesting carrots.

example:

```
let n = item.num_items(item.Carrot)
```

## game.item.Pumpkin

`game.item.Pumpkin : game.item.Item`

Gathered by harvesting pumpkins.

example:

```
let n = item.num_items(item.Pumpkin)
```

## game.item.Power

`game.item.Power : game.item.Item`

Gathered by harvesting sunflowers.

example:

```
let n = item.num_items(item.Power)
```

## game.item.Gold

`game.item.Gold : game.item.Item`

Gathered by harvesting treasure in mazes.

example:

```
let n = item.num_items(item.Gold)
```

## game.item.Bones

`game.item.Bones : game.item.Item`

Gathered from dinosaurs in mazes.

example:

```
let n = item.num_items(item.Bones)
```

## game.item.Water

`game.item.Water : game.item.Item`

Used with `item.use_item` to water crops.

example:

```
let _ = item.use_item(item.Water)
```

## game.item.Fertilizer

`game.item.Fertilizer : game.item.Item`

Used with `item.use_item` to speed up crop growth.

example:

```
let _ = item.use_item(item.Fertilizer)
```
