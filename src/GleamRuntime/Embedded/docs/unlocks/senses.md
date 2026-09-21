# Senses

The drone can see now!

`game.get_pos()` returns the current x and y position of the drone as a `Position`. At the start position they are both `0`. The x position increases by `1` every tile towards `game.East` and the y position increases by `1` every tile towards `game.North`.

```gleam
let pos = game.get_pos()
echo pos.x
echo pos.y
```

`item.num_items(item)` returns how many of an item you have.
For example `item.num_items(item.Hay)` returns how much hay you have.

```gleam
import game/item as item

item.num_items(item.Hay)
```

`game.get_entity_type()` and `game.get_ground_type()` return the type of entity or ground that is under the drone.

Do a flip if you are over a bush:

```gleam
case game.get_entity_type() {
  Some(game.Bush) -> game.do_a_flip()
  _ -> Nil
}
```

`game.get_entity_type()` returns `None` if there is no entity under the drone.

If you want to find out how many of a particular unlock you have, use the `unlock.num_unlocked()` function.

For example, `unlock.num_unlocked(unlock.Speed)` will return the number of speed upgrades you have.

```gleam
import game/unlock as unlock

unlock.num_unlocked(unlock.Speed)
```

`unlock.num_unlocked(unlock.Senses)` will return `1` if senses are unlocked and `0` if they are not.

You can also use `item.num_unlocked_item()` on items. This will return `1` if it's unlocked otherwise `0`.

Be careful — `unlock.num_unlocked(unlock.Carrots)` will return the number of times it was unlocked/upgraded, while `item.num_unlocked_item(item.Carrot)` will only return `0` or `1`.