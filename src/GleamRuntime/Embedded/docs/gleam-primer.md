# Gleam primer

A few quick lessons for getting comfortable with Gleam. Adapted from the
official [Gleam tour](https://tour.gleam.run) (Apache-2.0).

## primer:Expressions and values

Everything in Gleam is an **expression**: it produces a value. There are no
statements, no `return`, and no `None`-style void — every function body is one
expression whose value is the function's result.

```gleam
let x = 1 + 2
let name = "Lucy"
let ready = True
```

Basic types:

- `Int` — whole numbers: `1`, `-42`
- `Float` — decimal numbers: `1.5`
- `String` — text: `"hello"`
- `Bool` — `True` / `False`

A simple function — note the body is just the expression `a + b`:

```gleam
pub fn add(a: Int, b: Int) -> Int {
  a + b
}
```

## primer:Functions

Functions are declared with `fn`, and `pub fn` makes them callable from other
modules. Parameters and the return type are annotated.

```gleam
pub fn greet(name: String) -> String {
  "hello, " <> name
}
```

Arguments are passed in parentheses and can be **labelled** — `int.power(2,
of: 3.0)`. Some functions take a function as an argument; a tiny closure looks
like `fn(x) { x * 2 }`.

Because every open code window is an importable module, you can split helpers
into their own window and `import` them:

```gleam
import utils

pub fn main() {
  utils.visit_all(3, game.till)
}
```

(Helpers you call from another module need to be `pub fn`.)

## primer:Case expressions

Instead of `if`/`else`, Gleam uses `case` to match on values and patterns. This
is how you branch on the game's sensors and `Option`/`Result` values.

```gleam
case game.get_entity_type() {
  Some(game.Carrot) -> game.harvest()
  Some(_) -> game.move(game.North)
  None -> game.till()
}
```

`_` matches anything; a branch's body is an expression. `case` is exhaustive —
every possible value must be covered (a catch-all `_ -> ...` branch often helps).

## primer:Records and custom types

`pub type` declares a custom type with one or more constructors. A single
constructor with fields is a **record**:

```gleam
pub type Position {
  Position(x: Int, y: Int)
}

let p = Position(3, 4)
p.x // field access
let q = Position(..p, x: 10) // record update
```

Multiple constructors make a tagged union, which pairs naturally with `case`:

```gleam
pub type Weather { Sunny Windy | Rainy }
```

## primer:Pipelines and use

The `|>` pipe passes the previous value as the first argument of the next call,
reading left-to-right instead of inside-out:

```gleam
"  42  "
|> string.trim()
|> string.to_int()
```

`use` runs a function with a continuation. `use <- list.each(list, ...)` runs
the rest of the block once per element — a clean loop:

```gleam
use _ <- list.each(list.repeat(True, 3))
game.move(game.East)
```

## primer:Option and Result

Two custom types you will meet constantly. `Option` is a value that may be
absent — `Some(value)` or `None`. `Result` is a value that may have failed —
`Ok(value)` or `Error(reason)`.

The `game` sensors return options (`get_entity_type()` can be `None` on an
empty tile); `int.parse` and friends return results.

```gleam
case game.measure() {
  Some(growth) -> echo "growing"
  None -> game.till()
}

case int.parse("42") {
  Ok(n) -> n
  Error(_) -> 0
}
```

Decode a `Dynamic` from the bridge (drones, messages, results) with
`gleam/dynamic/decode`:

```gleam
case decode.run(value, decode.int) {
  Ok(n) -> n
  Error(_) -> 0
}
```