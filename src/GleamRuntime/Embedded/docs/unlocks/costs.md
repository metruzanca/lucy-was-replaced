# Costs

Any cost can be represented as a list of item pairs. `game.get_cost()` returns such a list: `List(#(item.Item, Int))`. It returns the cost of a plant.

```gleam
import game
import game/item
import gleam/list

let cost = game.get_cost(game.Pumpkin)
use #(item_, amount) <- list.each(cost)
echo #(item_, amount)
```

`game.get_cost(game.Pumpkin)` returns a list where each entry is `#(item, amount)`, for example `[#(item.Carrot, 1)]`.

For unlocks that are already at the max level, `game.get_cost()` returns an empty list.

Unlock costs are not yet queryable from Gleam — check the unlock's page in the docs window instead.