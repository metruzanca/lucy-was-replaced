# Mazes

Weird Substance, which is obtained by fertilizing plants, has a strange effect on bushes — it grows them into a maze of hedges. Gleam programs can't create mazes yet, but you can explore and solve the ones you have.

The drone can't fly over the hedges, even though they don't look that high.

There is a treasure hidden somewhere in the hedge. Use `game.harvest()` on the treasure to receive gold equal to the area of the maze. (For example, a 5x5 maze will yield 25 gold.)

If you use `game.harvest()` anywhere else the maze will simply disappear.

`game.get_entity_type()` returns `Some(game.Treasure)` if the drone is over the treasure and `Some(game.Hedge)` everywhere else in the maze.

```gleam
case game.get_entity_type() {
  Some(game.Treasure) -> game.harvest()
  _ -> game.move(game.North)
}
```

Mazes do not contain any loops unless you reuse the maze. So there is no way for the drone to end up in the same position again without going back.

You can check if there is a wall by trying to move through it.
`game.move()` returns `True` if it succeeded and `False` otherwise.

`game.can_move()` can be used to check if there is a wall without moving.

If you have no idea how to get to the treasure, take a look at Hint 1. It shows you how to approach a problem like this.

Using `game.measure()` anywhere in the maze returns the position of the treasure.

```gleam
case game.measure() {
  Some(game.MeasurePosition(position)) -> position
  _ -> game.get_pos()
}
```

<spoiler=show hint 1>Here's a general approach to solving the problem:

Create a maze and imagine that you are the drone.

Think about how you would try to find the treasure if you were in the maze.

Write down your strategy step by step so that someone else could follow it without thinking.

Now try translating your steps into code.
</spoiler>
<spoiler=show hint 2>As long as there are no loops: All the walls are really just one large connected wall. If you follow the wall, it will lead you through the whole maze.
This approach requires very little code and you do not need to keep track of where you have already been. Around 10 lines of code is all you need.</spoiler>
<spoiler=show hint 3>Instead of moving the drone in absolute directions like east or west it can be very useful to move the drone in relative directions like "turn right" or "turn left". To do this you need to keep track of which way the drone is currently moving. The drone never actually rotates, but you can still keep a "virtual" rotation in code.

```gleam
let directions = [game.North, game.East, game.South, game.West]
```

Use `% 4` to allow it to rotate "around the circle", so that after `game.West` it wraps back to `game.North`.

```gleam
// turn right
let index = (index + 1) % 4

// turn left
let index = (index + 3) % 4
let direction = directions
  |> list.drop(up_to: index)
  |> list.first
case direction {
  Ok(dir) -> {
    let _ = game.move(dir)
    Nil
  }
  Error(_) -> Nil
}
```</spoiler>
<spoiler=show hint 4>If you can't solve it, you can always make your life easy and do it less efficiently.
Solving a `1`x`1` maze is trivial.</spoiler>