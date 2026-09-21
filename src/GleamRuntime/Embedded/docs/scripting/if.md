# If

Gleam has no `if`/`else` statements. Conditionals are `case` expressions: you match a condition against `True` and `False`.

```gleam
case condition {
  True -> game.do_a_flip()
  False -> game.harvest()
}
```

## Syntax

`case` runs code only if some condition is `True`.

```gleam
// do a flip if condition is True
case condition {
  True -> game.do_a_flip()
  False -> Nil
}
```

A `case` is an expression, so it always produces a value. Every branch must have the same type, and every value must be covered — a catch-all `_` branch often helps.

Do a flip if `condition` is `True`, otherwise harvest:

```gleam
case condition {
  True -> game.do_a_flip()
  False -> game.harvest()
}
```

`True ->` and `False ->` are the replacement for `if`/`else`. For several conditions in order, match on a tuple:

```gleam
case #(condition1, condition2) {
  #(True, _) -> game.do_a_flip()
  #(False, True) -> game.till()
  _ -> game.clear()
}
```

## Matching on values

Where Python uses `elif` chains, `case` shines when you match on a value directly — like the entity under the drone:

```gleam
case game.get_entity_type() {
  Some(game.Carrot) -> game.harvest()
  Some(_) -> game.move(game.North)
  None -> game.till()
}
```

`case` is exhaustive: every possible value must be covered, so you can't forget a branch.