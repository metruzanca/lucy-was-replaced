# Dictionaries

Dictionaries are a datastructure that allows you to map keys to values in the same way that a real dictionary maps words to their definitions, and you can look them up very quickly.

A dictionary can be created with `dict.new()` and filled with `dict.insert()`. Every operation returns a new dictionary.

```gleam
import gleam/dict

let right_of = dict.new()
  |> dict.insert(game.North, game.East)
  |> dict.insert(game.East, game.South)
  |> dict.insert(game.South, game.West)
  |> dict.insert(game.West, game.North)
```

The above dictionary maps each direction to the direction to its right.

Accessing the value mapped to a key returns a `Result` — `Ok(value)` if the key is present, `Error(Nil)` otherwise.

```gleam
dict.get(right_of, game.South)
```

To provide a default value when the key is missing, match on the result:

```gleam
case dict.get(right_of, game.South) {
  Ok(value) -> value
  Error(_) -> game.North
}
```

Use `dict.insert(dict, key, value)` to add or overwrite a key-value pair. Keys are unique, so adding a key that already exists overwrites the previous value.

Use `dict.delete(dict, key)` to remove a key-value pair.

`dict.has_key(dict, key)` returns `True` if `key` is a key in the `dict` and `False` otherwise.

Iterating a dictionary with `use` gives you every key and value:

```gleam
use key, value <- dict.each(right_of)
echo #(key, value)
```

There are no guarantees about the order in which the keys are iterated.

See also [Sets](docs/scripting/sets.md)