# While Loop

You have unlocked the values `True` and `False`, and the ability to repeat code.

Gleam has no `while` statement. To keep running code until a condition becomes `False`, write a recursive function with a `case` — a self-recursive call compiles to a `while(true)` loop.

## For Beginners

Perhaps you have already tried to put several `game.harvest()` calls in a row:

```gleam
game.harvest()
game.harvest()
game.harvest()
```

This allows you to harvest several times in one program run.
However, it would be nice to harvest more than three times, and writing the same code multiple times is bad practice.
The solution is a loop.
A loop allows you to run the same code multiple times.

A condition is a logical value that can only be in one of two states: `True` or `False`.
Such a value is called a Boolean value.

Here is a loop that harvests while `game.can_harvest()` returns `True`:

```gleam
pub fn harvest_all() {
  case game.can_harvest() {
    True -> {
      game.harvest()
      harvest_all()
    }
    False -> Nil
  }
}
```

The function runs the block while the condition is `True` and returns when it becomes `False`.

There are two constant boolean values available. Constants are values that never change during the program.

To create a constant boolean value that is always `True`, you can simply write `True`. Write `False` as a constant boolean value that will always be `False`.
So you could either write a loop that never runs:

```gleam
case False {
  True -> game.do_a_flip()
  False -> Nil
}
```

or one that flips forever:

```gleam
pub fn flip_forever() {
  game.do_a_flip()
  flip_forever()
}
```

The first will never do a flip and the second will do flips forever (an infinite loop).

Normally creating an infinite loop is a bad idea because it will freeze the program, but in this game there are delays between each iteration of the loop, so it will cause the drone to keep doing a flip until you manually stop it by pressing the execute button again.

An unconditional loop is just a recursive function that always recurses — like `flip_forever` above.