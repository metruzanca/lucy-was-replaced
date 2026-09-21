# Functions

Use `pub fn` to define a new function:

```gleam
pub fn f(arg1: Int, arg2: Bool) -> Nil {
  // function code
}
```

You can use the call operator `()` to call the function:

```gleam
f(42, False)
```

Also see [Scopes](docs/scripting/scopes.md) to learn about local and global values in functions.

## Introduction

You've already seen built-in functions like `game.harvest()`.
You can also define your own functions which allows structuring your code in a modular way. It basically allows you to give a name to a block of code so you can call it from anywhere you want.

## Function Definitions

For example, you could define a function that moves the drone several times.

```gleam
import gleam/list

pub fn move_n_dir(n: Int, dir: game.Direction) {
  use _ <- list.each(list.repeat(True, n))
  game.move(dir)
}
```

`pub fn` signals that this is a function definition.
`move_n_dir` is the name of the function. This can be any valid name and will be used to call the function.
`n` and `dir` are parameters. They are variables that hold the values that are passed into the function (These values are also called arguments). You can add as many parameters to a function definition as you want.
The `{ ... }` contains the code block that will run when the function is called.

With the above definition the following code then moves the drone `10` tiles `game.North` and `2` tiles `game.West`.

```gleam
move_n_dir(10, game.North)
move_n_dir(2, game.West)
```

## Return Values

A function's body is one expression, and its value is the function's result — there is no `return` keyword.

For example, the following function defines the exclusive or operation. The exclusive or returns `True` if one value is `True` and the other one is `False`:

```gleam
pub fn xor(a: Bool, b: Bool) -> Bool {
  a != b
}

case xor(True, False) {
  True -> game.do_a_flip()
  False -> Nil
}
```

[Tuples](docs/scripting/tuples.md) allow returning multiple values.

```gleam
pub fn f() -> #(Int, Int) {
  #(1, 2)
}
```

## Functions as Values

Functions are values just like any other value, and a `fn` is a closure you can pass around.

```gleam
pub fn flip() {
  game.do_a_flip()
}

pub fn many_times(count: Int, action: fn() -> Nil) {
  use _ <- list.each(list.repeat(True, count))
  action()
}

many_times(10, flip)
```

A tiny inline closure looks like `fn() { game.do_a_flip() }`.

## Helpers Must Be `pub fn`

Every open code window is a module. To call a function from another window, it must be `pub fn` — a private `fn` is a compile error at the use site.