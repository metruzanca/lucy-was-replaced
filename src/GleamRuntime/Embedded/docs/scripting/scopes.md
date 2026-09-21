# Scopes

Scopes determine which values can be accessed from where.

Gleam has no mutable global variables. A window's top-level `let` bindings and `pub fn` definitions are module-level; `let` bindings inside a function are block-scoped and live only until the end of their block.

```gleam
let x = 1
```

Assigns a value of `1` to the name `x` at the top of the module.

```gleam
pub fn f() {
  let y = 1
  // y only exists inside f
}
```

`y` only exists inside `f`. There is no `global` keyword, because there is nothing to mutate — functions can't accidentally overwrite a module-level binding.

To share a value, pass it as a parameter:

```gleam
pub fn g(y: Int) -> Int {
  y + 1
}
```

## Closures capture

A `fn` defined inside another function captures the surrounding values. That's how you share state without globals:

```gleam
pub fn make_counter(start: Int) -> fn() -> Int {
  fn() { start + 1 }
}
```

## Shadowing

You can reuse a name by shadowing it with a new `let`. The new binding only exists from that point on:

```gleam
let x = 1
let x = x + 1
// x is now 2 here
```

## Blocks

`let ... { ... }` runs a block with a value in scope:

```gleam
let y = 3
let result = {
  let y = y + 1
  y * 2
}
// result is 8; the outer y is still 3
```