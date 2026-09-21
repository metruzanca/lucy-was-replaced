# GleamFarmer — the `game` library

The in-game reference for *Lucy was Replaced*. Every open code window is an
importable Gleam module; `import game` gives you the farm. This file is the
single source of truth for the in-game docs panel — it is split on `## `
headings into the game's doc pages.

## game module

`import game` gives typed access to the world: the drone, the farm, movement,
planting, harvesting, sensors, and utilities (swap, clear, measure, costs,
progression, cosmetics). Items live in a second module: `import game/item`,
then `item.num_items(item.Hay)`. The research tree lives in a third: `import
game/unlock`, then `unlock.unlock(unlock.Megafarm)`.

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
- `game.Hat` — `StrawHat`, `DinosaurHat`, `GreenHat`, `GrayHat`, `PurpleHat`, `BrownHat`, `WizardHat`, `TopHat`, `TrafficCone`, `TrafficConeStack`, `PumpkinHat`, `CarrotHat`, `CactusHat`, `SunflowerHat`, `GoldHat`, `GoldenGoldHat`, `TreeHat`, `GoldTrophyHat`, `SilverTrophyHat`, `WoodTrophyHat`, `GoldenCactusHat`, `GoldenCarrotHat`, `GoldenPumpkinHat`, `GoldenSunflowerHat`, `GoldenTreeHat`, `TheFarmersRemains`
- `game.Position` — `Position(x: Int, y: Int)`
- `game.Measure` — `MeasureValue(value: Int)` / `MeasurePosition(position: game.Position)`
- `game.Companion` — `Companion(entity: game.Entity, position: game.Position)`
- `game.item.Item` — `Hay`, `Wood`, `Carrot`, `Pumpkin`, `Power`, `Gold`, `Bones`, `Water`, `Fertilizer`
- `game.unlock.Unlock` — `AutoUnlock`, `Cactus`, `Carrots`, `Costs`, `Debug`, `Debug2`, `Dictionaries`, `Dinosaurs`, `Expand`, `Fertilizer`, `Functions`, `Grass`, `Hats`, `Import`, `Leaderboard`, `Lists`, `Loops`, `Mazes`, `Megafarm`, `Operators`, `Plant`, `Polyculture`, `Pumpkins`, `Senses`, `Simulation`, `Speed`, `Sunflowers`, `TheFarmersRemains`, `Timing`, `TopHat`, `Trees`, `Utilities`, `Variables`, `Watering`
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

- `range(n)` → repeat a block `n` times with `use _ <- list.each(list.repeat(True, n))`
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

## game.unlock.unlock

`game.unlock.unlock(unlock: game.unlock.Unlock) -> Bool`

Spend resources to unlock (or upgrade) a feature in the research tree, e.g.
`unlock.Megafarm` or `unlock.Carrots`. Has exactly the same effect as clicking
the button in the research tree.

returns `True` if the unlock was successful, `False` otherwise.

takes `200` ticks to execute if it succeeded, `1` tick otherwise.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Megafarm)
```

## game.unlock.num_unlocked

`game.unlock.num_unlocked(unlock: game.unlock.Unlock) -> Int`

How many times an unlock has been bought (`0` = not unlocked). For upgradeable
unlocks this is `1` plus the number of upgrades.

takes `1` tick to execute.

example:

```
import game/unlock

let level = unlock.num_unlocked(unlock.Mazes)
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

`game.change_hat(hat: game.Hat) -> Nil`

Changes the hat of the drone to `hat` (e.g. `game.StrawHat`). Hats are cosmetic
except for the dinosaur hat, which starts the dinosaur game.

takes `200` ticks to execute.

example:

```
game.change_hat(game.DinosaurHat)
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

## game.StrawHat

`game.StrawHat : game.Hat`

The default hat.

example:

```
game.change_hat(game.StrawHat)
```

## game.DinosaurHat

`game.DinosaurHat : game.Hat`

Equip it to start the dinosaur game.

example:

```
game.change_hat(game.DinosaurHat)
```

## game.GreenHat

`game.GreenHat : game.Hat`

A green hat.

example:

```
game.change_hat(game.GreenHat)
```

## game.GrayHat

`game.GrayHat : game.Hat`

A gray hat.

example:

```
game.change_hat(game.GrayHat)
```

## game.PurpleHat

`game.PurpleHat : game.Hat`

A purple hat.

example:

```
game.change_hat(game.PurpleHat)
```

## game.BrownHat

`game.BrownHat : game.Hat`

A brown hat.

example:

```
game.change_hat(game.BrownHat)
```

## game.WizardHat

`game.WizardHat : game.Hat`

A magical wizard hat. Unlocking it must have taken some programming magic!

example:

```
game.change_hat(game.WizardHat)
```

## game.TopHat

`game.TopHat : game.Hat`

It looks expensive and distinguished.

example:

```
game.change_hat(game.TopHat)
```

## game.TrafficCone

`game.TrafficCone : game.Hat`

Safety first!

example:

```
game.change_hat(game.TrafficCone)
```

## game.TrafficConeStack

`game.TrafficConeStack : game.Hat`

A stack of traffic cones as a hat. Safety first, several times over.

example:

```
game.change_hat(game.TrafficConeStack)
```

## game.PumpkinHat

`game.PumpkinHat : game.Hat`

A pumpkin for your head. Perfect for Halloween.

example:

```
game.change_hat(game.PumpkinHat)
```

## game.CarrotHat

`game.CarrotHat : game.Hat`

Who wouldn't want carrots on their head?

example:

```
game.change_hat(game.CarrotHat)
```

## game.CactusHat

`game.CactusHat : game.Hat`

A bit spiky.

example:

```
game.change_hat(game.CactusHat)
```

## game.SunflowerHat

`game.SunflowerHat : game.Hat`

Bright and cheerful.

example:

```
game.change_hat(game.SunflowerHat)
```

## game.GoldHat

`game.GoldHat : game.Hat`

Show off your wealth with this golden hat.

example:

```
game.change_hat(game.GoldHat)
```

## game.GoldenGoldHat

`game.GoldenGoldHat : game.Hat`

It's even more golden than the normal gold hat.

example:

```
game.change_hat(game.GoldenGoldHat)
```

## game.TreeHat

`game.TreeHat : game.Hat`

A hat shaped like a tree. How nice!

example:

```
game.change_hat(game.TreeHat)
```

## game.GoldTrophyHat

`game.GoldTrophyHat : game.Hat`

A golden trophy hat. Only for the best farmer.

example:

```
game.change_hat(game.GoldTrophyHat)
```

## game.SilverTrophyHat

`game.SilverTrophyHat : game.Hat`

A silver trophy hat. For the second best farmer.

example:

```
game.change_hat(game.SilverTrophyHat)
```

## game.WoodTrophyHat

`game.WoodTrophyHat : game.Hat`

A wooden trophy hat. For the third best farmer.

example:

```
game.change_hat(game.WoodTrophyHat)
```

## game.GoldenCactusHat

`game.GoldenCactusHat : game.Hat`

A golden hat shaped like a cactus.

example:

```
game.change_hat(game.GoldenCactusHat)
```

## game.GoldenCarrotHat

`game.GoldenCarrotHat : game.Hat`

A golden hat shaped like a carrot.

example:

```
game.change_hat(game.GoldenCarrotHat)
```

## game.GoldenPumpkinHat

`game.GoldenPumpkinHat : game.Hat`

A golden hat shaped like a pumpkin.

example:

```
game.change_hat(game.GoldenPumpkinHat)
```

## game.GoldenSunflowerHat

`game.GoldenSunflowerHat : game.Hat`

A golden hat shaped like a sunflower.

example:

```
game.change_hat(game.GoldenSunflowerHat)
```

## game.GoldenTreeHat

`game.GoldenTreeHat : game.Hat`

A golden hat shaped like a tree.

example:

```
game.change_hat(game.GoldenTreeHat)
```

## game.TheFarmersRemains

`game.TheFarmersRemains : game.Hat`

The remains of a farmer.

example:

```
game.change_hat(game.TheFarmersRemains)
```

## game.unlock.AutoUnlock

`game.unlock.AutoUnlock : game.unlock.Unlock`

Automatically unlock things.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.AutoUnlock)
```

## game.unlock.Cactus

`game.unlock.Cactus : game.unlock.Unlock`

Unlocks cactus. Upgrades increase the yield and cost of cactus.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Cactus)
```

## game.unlock.Carrots

`game.unlock.Carrots : game.unlock.Unlock`

Unlocks tilling the soil and planting carrots. Upgrades increase the yield and
cost of carrots.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Carrots)
```

## game.unlock.Costs

`game.unlock.Costs : game.unlock.Unlock`

Allows access to the cost of things.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Costs)
```

## game.unlock.Debug

`game.unlock.Debug : game.unlock.Unlock`

Tools to help with debugging programs.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Debug)
```

## game.unlock.Debug2

`game.unlock.Debug2 : game.unlock.Unlock`

Functions to temporarily slow down the execution and make the grid smaller.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Debug2)
```

## game.unlock.Dictionaries

`game.unlock.Dictionaries : game.unlock.Unlock`

Get access to dictionaries and sets.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Dictionaries)
```

## game.unlock.Dinosaurs

`game.unlock.Dinosaurs : game.unlock.Unlock`

Unlocks majestic ancient creatures. Upgrades increase the yield and cost of
dinosaurs.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Dinosaurs)
```

## game.unlock.Expand

`game.unlock.Expand : game.unlock.Unlock`

Unlocks expanding the farm land and unlocks movement. Upgrades expand the farm.
This also clears the farm.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Expand)
```

## game.unlock.Fertilizer

`game.unlock.Fertilizer : game.unlock.Unlock`

Reduces the remaining growing time of the plant under the drone by 2 seconds.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Fertilizer)
```

## game.unlock.Functions

`game.unlock.Functions : game.unlock.Unlock`

Define your own functions.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Functions)
```

## game.unlock.Grass

`game.unlock.Grass : game.unlock.Unlock`

Increases the yield of grass.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Grass)
```

## game.unlock.Hats

`game.unlock.Hats : game.unlock.Unlock`

Unlocks new hat colors for your drone.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Hats)
```

## game.unlock.Import

`game.unlock.Import : game.unlock.Unlock`

Import code from other files.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Import)
```

## game.unlock.Leaderboard

`game.unlock.Leaderboard : game.unlock.Unlock`

Join the leaderboard for the fastest time in farming a specific crop or for the
fastest reset of the farm.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Leaderboard)
```

## game.unlock.Lists

`game.unlock.Lists : game.unlock.Unlock`

Use lists to store lots of values.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Lists)
```

## game.unlock.Loops

`game.unlock.Loops : game.unlock.Unlock`

Unlocks a simple while loop.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Loops)
```

## game.unlock.Mazes

`game.unlock.Mazes : game.unlock.Unlock`

Unlocks a maze with a treasure in the middle. Upgrades increase the gold in
treasure chests.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Mazes)
```

## game.unlock.Megafarm

`game.unlock.Megafarm : game.unlock.Unlock`

Unlocks multiple drones and drone management functions.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Megafarm)
```

## game.unlock.Operators

`game.unlock.Operators : game.unlock.Unlock`

Arithmetic, comparison and logic operators.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Operators)
```

## game.unlock.Plant

`game.unlock.Plant : game.unlock.Unlock`

Unlocks planting.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Plant)
```

## game.unlock.Polyculture

`game.unlock.Polyculture : game.unlock.Unlock`

Use companion planting to increase the yield.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Polyculture)
```

## game.unlock.Pumpkins

`game.unlock.Pumpkins : game.unlock.Unlock`

Unlocks pumpkins. Upgrades increase the yield and cost of pumpkins.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Pumpkins)
```

## game.unlock.Senses

`game.unlock.Senses : game.unlock.Unlock`

The drone can see what's under it and where it is.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Senses)
```

## game.unlock.Simulation

`game.unlock.Simulation : game.unlock.Unlock`

Unlocks simulation functions for testing and optimization.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Simulation)
```

## game.unlock.Speed

`game.unlock.Speed : game.unlock.Unlock`

Increases the speed of the drone.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Speed)
```

## game.unlock.Sunflowers

`game.unlock.Sunflowers : game.unlock.Unlock`

Unlocks sunflowers and power. Upgrades increase the power gained from
sunflowers.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Sunflowers)
```

## game.unlock.TheFarmersRemains

`game.unlock.TheFarmersRemains : game.unlock.Unlock`

Unlocks the special hat 'The Farmers Remains'.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.TheFarmersRemains)
```

## game.unlock.Timing

`game.unlock.Timing : game.unlock.Unlock`

Functions to help measure performance.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Timing)
```

## game.unlock.TopHat

`game.unlock.TopHat : game.unlock.Unlock`

Unlocks the fancy Top Hat.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.TopHat)
```

## game.unlock.Trees

`game.unlock.Trees : game.unlock.Unlock`

Unlocks trees. Upgrades increase the yield of bushes and trees.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Trees)
```

## game.unlock.Utilities

`game.unlock.Utilities : game.unlock.Unlock`

Unlocks the `min()`, `max()` and `abs()` functions.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Utilities)
```

## game.unlock.Variables

`game.unlock.Variables : game.unlock.Unlock`

Assign values to variables.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Variables)
```

## game.unlock.Watering

`game.unlock.Watering : game.unlock.Unlock`

Water the plants to make them grow faster.

example:

```
import game/unlock

let _ = unlock.unlock(unlock.Watering)
```
