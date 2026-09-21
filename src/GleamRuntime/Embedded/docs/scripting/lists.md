# Lists

Lists are an easy way to store multiple values in a single variable.
You can create new lists like this:

```gleam
let some_list = [2, 3, 4]
let names = ["hay", "wood", "carrot"]
```

A list can also be empty:

```gleam
let empty_list = []
```

A list can only hold values of one type.

## Accessing elements

Gleam lists are linked lists, so there is no index operator. Pattern matching is the idiomatic way to take a list apart:

```gleam
case my_list {
  [first, ..rest] -> echo first
  [] -> echo "empty"
}
```

`list.first` and `list.rest` do the same thing and return a `Result`:

```gleam
list.first(my_list) // Ok(first) or Error(Nil)
list.rest(my_list)  // Ok(rest) or Error(Nil)
```

## Building lists

The `..` spread prepends to the front of a list, which is cheap:

```gleam
let numbers = [1, 2, 3]
let more = [0, ..numbers]   // [0, 1, 2, 3]
```

`list.append` joins two lists:

```gleam
list.append(numbers, [4, 5]) // [1, 2, 3, 4, 5]
```

Lists are immutable: every operation returns a new list, and the original is unchanged.

## Iterating

You can iterate over a list using `use`. The following example sums all the elements in the list.

```gleam
import gleam/list

let numbers = [4, 7, 2, 5]
let total = list.fold(numbers, over: 0, with: fn(total, n) { total + n })
```

`total` is now `18`.

To run a block once per element:

```gleam
use n <- list.each(numbers)
echo n
```

## Other useful functions

`list.length(list)` returns the number of elements.

`list.contains(list, elem)` returns `True` if the list contains `elem`.

`list.map`, `list.filter` and `list.fold` transform and combine lists.