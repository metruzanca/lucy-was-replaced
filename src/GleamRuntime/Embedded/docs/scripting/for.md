# For Loop

Gleam has no `for` loop. The same effect comes from iterating over a list with `use` — one iteration of the rest of the block per element.

To repeat a block a fixed number of times, build a list with `list.repeat` and iterate it:

```gleam
import gleam/list

use _ <- list.each(list.repeat(True, 5))
game.harvest()
```

This calls `game.harvest()` exactly `5` times — the rest of the enclosing function runs once per element.

To do something with each element of a list, bind it with `use`:

```gleam
import gleam/list

use x <- list.each([1, 2, 3, 4, 5])
echo x
```

## Sequences

Iterable values are the same as in Python: [Lists](docs/scripting/lists.md), <unlock=functions>[Tuples](docs/scripting/tuples.md)      </unlock><unlock=dicts>[Dictionaries](docs/scripting/dicts.md)      </unlock><unlock=sets>[Sets](docs/scripting/sets.md)</unlock>

## Example

```gleam
import gleam/list

use _ <- list.each(list.repeat(True, 5))
game.harvest()
```

This loop executes the body a fixed number of times. It is essentially the same as writing `game.harvest()` five times.

See also [Break](docs/scripting/break.md) and [Continue](docs/scripting/continue.md)