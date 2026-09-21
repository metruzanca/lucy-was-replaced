# Tuples

Tuples are a great way to combine multiple values into a single value.
To create a tuple, write the values separated by a comma in parentheses:

```gleam
let pos = #(1, 2)
```

You can unpack them into several variables again with `let`:

```gleam
let #(a, b) = #(1, 2)
```

`a` is now `1` and `b` is now `2`.

Tuples are immutable and cannot be changed after creation. `pair.first` and `pair.second` read the two elements:

```gleam
pair.first(#(1, 2))  // 1
pair.second(#(1, 2)) // 2
```

They can be useful for returning multiple values from a function:

```gleam
pub fn f() -> #(Int, Int) {
  #(1, 2)
}

let #(a, b) = f()
```

The game's `game.get_pos()` returns a `Position` record with `x` and `y` fields, and `game.get_companion()` returns a `Companion` record — both easier to read than a bare tuple.