# Expand 2

Your farm has expanded again! Now the tiles are no longer in a nice row, so you need to find a way to traverse a square grid.

Gleam has no `for` loop. To repeat a block a fixed number of times, iterate over a list with `use`:

```gleam
import gleam/list

use _ <- list.each(list.repeat(True, 5))
game.do_a_flip()
```

This runs the rest of the block once per element. `list.repeat(True, n)` creates a list of `n` `True` values, so the block runs `n` times. In this example `game.do_a_flip()` will be called `5` times.

The function `game.get_world_size()` is also available now. It returns the side length of your farm. This way you can write code that won't break with the next expand upgrade.

```gleam
import gleam/list

pub fn harvest_column() {
  use _ <- list.each(list.repeat(True, game.get_world_size()))
  game.harvest()
  game.move(game.North)
}
```

This example harvests one column of the farm for any farm size.

If you're stuck on trying to figure out how to move the drone around the farm see the hint below.
<spoiler=show hint>There are, of course, several ways to move around the farm.
What we're looking for is a way to traverse it in a systematic way that won't break when the farm grows again.
A systematic way to get to every place on the farm would be to repeat the following 2 steps forever:

1. Move `game.North` until it wraps back.
2. Move `game.East`

Nested `use` blocks may be helpful to turn this idea into code.
</spoiler>
<spoiler=show possible solution> The basic traversal might look like this:

```gleam
import gleam/list

pub fn traverse() {
  use _ <- list.each(list.repeat(True, game.get_world_size()))
  use _ <- list.each(list.repeat(True, game.get_world_size()))
  game.do_a_flip()
  game.move(game.North)
  game.move(game.East)
}
```
</spoiler>