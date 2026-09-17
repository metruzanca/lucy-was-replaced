# Gleam Curriculum (for The Farmer Was Replaced)

The language course, paced to the game's world progression. Each stage pairs the world unlocks
you just bought with the Gleam concepts that make the most sense there. Work through them in
order — later stages assume the earlier ones.

> The full language is always available; these are the concepts we *surface* at each stage so you
> learn them when they're useful, not all at once.

---

## Stage 0 — One tile: the shape of a Gleam program

**World:** harvest grass, buy `Plant` and `Senses`.

**Concepts:** `pub fn main()`, `import`, `let` bindings, `io.println`, string concatenation `<>`,
integers, `case`, and the first sensors.

```gleam
import game
import gleam/io
import gleam/option.{None, Some}

pub fn main() {
  let name = "my first farm"
  io.println("hello, " <> name)

  case game.get_entity_type() {
    None -> io.println("bare ground")
    Some(game.Grass) -> io.println("grass under me")
    Some(_) -> io.println("something else")
  }
}
```

Try: `examples/stage0.gleam`. **Recursion preview:** `pub fn main() { Nil }` is already a function
call — everything in Gleam is built from functions.

## Stage 1 — Movement & soil: recursion instead of loops

**World:** `Expand`, `Carrots`, `Watering`.

**Concepts:** functions with parameters, **recursion** (there are no `while`/`for` loops in Gleam),
`case` on custom types, `game.move/till/plant`, `game.get_pos()`.

```gleam
import game
import gleam/io
import gleam/int

pub fn main() {
  plant_row(3)          // recursion: no loops needed
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
      plant_row(n - 1)   // the "loop" is a function calling itself
    }
  }
}
```

**Key idea:** loops are written as recursive functions. The base case (`0 -> Nil`) stops the
recursion; the recursive case does one step and calls itself with the rest.

## Stage 2 — Wood & speed: lists and the inventory

**World:** `Trees`, `Grass`, `Speed`.

**Concepts:** list literals `[a, b, c]`, `gleam/list` (`length`, `map`, `fold`), the inventory
(`game/item.num_items`), `int.to_string`, comparisons.

```gleam
import game
import game/item
import gleam/io
import gleam/int
import gleam/list

pub fn main() {
  let crops = [item.Carrot, item.Pumpkin, item.Hay]
  let total = list.fold(crops, 0, fn(acc, c) { acc + item.num_items(c) })
  io.println("harvest count: " <> int.to_string(total))
}
```

Try: `examples/stage2.gleam`.

## Stage 3 — Pumpkins & fertilizer: functions as values, then pipelines

**World:** `Pumpkins`, `Fertilizer`.

**Concepts:** passing `fn` values, `Option`/`Result`, **pipelines `|>`**, `string.join`.

```gleam
import game
import game/item
import gleam/io
import gleam/int
import gleam/list

pub fn main() {
  let hay = item.num_items(item.Hay)
  let report = hay |> int.to_string |> fn(s) { "hay: " <> s }
  io.println(report)

  let squares = list.map([1, 2, 3], fn(x) { x * x })
  io.println(int.to_string(list.length(squares)))
}
```

**Key idea:** `x |> f |> g` is `g(f(x))` — data flowing through steps, read left to right. This is
the first "prettier" syntax; you could write the same thing with nested calls, but pipelines read
like a recipe.

## Stage 4 — Megafarm: `use` and labelled arguments

**World:** `Megafarm`, drone management.

**Concepts:** `use` expressions (sequential effects), labelled arguments, multi-module `import`.

```gleam
import game
import game/item
import gleam/io
import gleam/int

pub fn main() {
  use hay <- with_value(item.num_items(item.Hay))
  io.println("hay: " <> int.to_string(hay))
}

fn with_value(value: Int, continuation: fn(Int) -> Nil) -> Nil {
  continuation(value)
}
```

**Key idea:** `use x <- f(...)` calls `f(..., fn(x) { rest })` — it threads a value through the
rest of the block without nesting. Great for sequenced drone work.

## Stage 5 — Non-linear crops: records & custom types

**World:** `Cactus`, `Sunflowers`, `Polyculture`.

**Concepts:** **records / custom types**, record updates, `gleam/dict`.

```gleam
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
```

Try: `examples/stage5.gleam`. Records group data; pattern matching on them is covered next stage.

## Stage 6 — Mazes & dinosaurs: algorithms in Gleam

**World:** `Mazes`, `Dinosaurs`.

**Concepts:** tail recursion, list algorithms (`filter`, `reverse`), `Result` error handling,
`panic` / `todo` as explicit placeholders.

```gleam
import gleam/io
import gleam/int
import gleam/list

pub fn main() {
  let evens = list.filter([1, 2, 3, 4, 5, 6], fn(x) { x % 2 == 0 })
  io.println(int.to_string(list.length(evens)))
}
```

## Stage 7 — Leaderboard: timing & optimisation

**World:** `Leaderboard`.

**Concepts:** `game.get_time` / `game.get_tick_count`, `set_execution_speed`, measuring a run.

```gleam
import game
import gleam/io
import gleam/int

pub fn main() {
  let start = game.get_tick_count()
  // … farm a bit …
  io.println("ticks used: " <> int.to_string(game.get_tick_count() - start))
}
```

---

## Notes
- Examples live in `examples/stage0.gleam` … `examples/stage7.gleam` and run headlessly with
  `./scripts/run-gleam.sh examples/stageN.gleam`.
- The `game` module covers movement, farming, sensors, utilities, costs and progression
  (`unlock`/`num_unlocked`/`get_cost`/`set_world_size`/…). A few declared sensors are still
  awaiting their runtime bridge (`measure`, `measure_at`, `get_companion`, `get_cost`) and
  `simulate`/`spawn_drone` are not yet exposed — the concepts above rely only on what works.