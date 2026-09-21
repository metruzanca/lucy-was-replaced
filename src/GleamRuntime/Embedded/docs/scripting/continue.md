# Continue

Gleam has no `continue` statement. Instead of skipping the rest of a loop iteration, you branch so that the skipped code never runs in the first place.

The Python way of skipping an iteration looks like this:

```python
while True:
    if not can_harvest():
        continue
    harvest()
```

In Gleam, put the code you want to skip inside the matching branch:

```gleam
case game.can_harvest() {
  True -> {
    let _ = game.harvest()
    Nil
  }
  False -> Nil
}
```

This only calls `game.harvest()` when `game.can_harvest()` is `True`.

The same idea works inside a recursive loop: each branch of a `case` either harvests, or skips straight to the next iteration.

```gleam
pub fn harvest_all() {
  case game.can_harvest() {
    True -> {
      let _ = game.harvest()
      harvest_all()
    }
    False -> harvest_all()
  }
}
```