# Sets

Sets are like [dictionaries](docs/scripting/dicts.md), but without values. They're just an unordered set of keys.

They are created with `set.new()` and filled with `set.insert()`. Every operation returns a new set.

```gleam
import gleam/set

let directions = set.new()
  |> set.insert(game.North)
  |> set.insert(game.East)
  |> set.insert(game.West)
```

Use `set.contains(set, elem)` to check if the set contains an element:

```gleam
set.contains(directions, game.East) // True
```

Use `set.delete(set, elem)` to remove an element from a set.

Iterating a set with `use` visits every element:

```gleam
use elem <- set.each(directions)
echo elem
```

For larger sets, membership checks perform much faster than they would on a list.

Just like dictionaries, sets are unordered, so there are no guarantees about the order in which the elements are iterated.

Also, elements in sets are unique, so adding an element that is already in the set will not change the set.