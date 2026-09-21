# Variables

Variables can be thought of as named containers that can hold a value.
`let` declares a variable and stores a value in it.

```gleam
let variable_name = value
```

The left hand side of the `=` is the variable name. You can give it any name you want.
The right hand side is an expression whose resulting value will be stored in the variable.

Declare a variable named `a` and store the value `5` in it:

```gleam
let a = 5
```

Declare a variable named `b` and store the return value of `game.can_harvest()` in it:

```gleam
let b = game.can_harvest()
```

Do not confuse the `=` operator with the `==` operator.
The `==` operator checks whether two values are equal and returns `True` or `False`.
The `=` operator assigns the value on the right to the name on the left.

After a variable has been assigned you can use it in the code to retrieve the value it contains.

```gleam
let a = 5
use _ <- list.each(list.repeat(True, a))
game.do_a_flip()
```

The block above runs 5 times because `a` is set to `5`.

## Changing a variable

Gleam has no mutable variables. To "change" a value, bind a new one with the same name — this is called shadowing:

```gleam
let i = 0
let i = i + 1
```

`i` is now `1`. From that point on, every use of `i` refers to the new value.

This is very common when tracking a running total or an index, so it reads much like the Python assignment `i = i + 1`.