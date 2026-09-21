# Expand 1

<unlock=for>Also see [Expand 2](docs/unlocks/expand_2.md)

</unlock>Your farm has grown! This space is not much use if you can't move the drone, so there is a new function `game.move()` that moves the drone. `game.move()` requires that you specify the direction in which you want the drone to move. There are four new constants for this: `game.North, game.East, game.South, game.West`.

For example, `game.move(game.North)` will move the drone one square to the north.

```gleam
game.move(game.North)
```

If you move over the edge of the farm the drone will be moved to the other side of the farm.
The following example code will keep moving the drone north and wrap back to the start when it reaches the edge of the farm:

```gleam
pub fn move_north_forever() {
  game.move(game.North)
  move_north_forever()
}
```