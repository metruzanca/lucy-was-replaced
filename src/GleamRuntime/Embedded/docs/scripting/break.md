# Break

Gleam has no `break` statement. Instead of stopping a loop early, you stop a loop by simply not continuing it — a recursive function returns when it's done.

The Python way of stopping a loop early looks like this:

```python
while True:
    if can_harvest():
        break
```

In Gleam, write a recursive function that returns when the condition is met:

```gleam
pub fn wait_until_ready() {
  case game.can_harvest() {
    True -> Nil
    False -> wait_until_ready()
  }
}
```

This keeps calling `game.can_harvest()` and returns (stops) as soon as it is `True`.

A self-recursive call like this compiles to a loop, so it doesn't grow the call stack.

A simpler example that stops after `done` becomes `True`:

```gleam
pub fn until_done() {
  case done {
    True -> Nil
    False -> until_done()
  }
}
```