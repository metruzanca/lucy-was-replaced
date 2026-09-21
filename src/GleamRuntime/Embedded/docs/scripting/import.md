# Import

Putting all your code in a single window quickly becomes unmanageable.
`import` statements allow you to pull functions and values from other files.

Every open code window is a module. `import utils` gives you everything that window defines.

```gleam
import utils

pub fn main() {
  utils.visit_all(3, fn() { game.till() })
}
```

Helpers you call from another window must be `pub fn`.

## The game library

`import game` gives you the farm. Its sub-modules use a `/` in their path, and are usually given a shorter name with `as`:

```gleam
import game
import game/item as item
import game/unlock as unlock
```

Then use them with that name:

```gleam
item.num_items(item.Water)
unlock.unlock(unlock.Megafarm)
```

## Selective imports

Sometimes you only want one thing from a module. List them in `{ }`:

```gleam
import game.{type Position, Position}
```

`Position` is both a **type** and its **record constructor** — and those are two different imports. `type Position` brings in the type, so you can write it in annotations (`-> Position`); `Position` brings in the constructor, so you can build a value (`Position(1, 2)`). When a name is both, you import it twice.

`Direction` works a little differently: it is a plain type whose constructors are `North`, `East`, `South`, `West`. To use the type and its constructors selectively:

```gleam
import game.{type Direction, North, East, South, West}
```

In practice `import game` (no `{ }`) brings in everything, so you rarely need selective imports for the game module — but they are handy for keeping your own windows tidy.