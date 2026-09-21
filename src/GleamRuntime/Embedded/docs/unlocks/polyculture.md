# Polyculture

You may have already noticed that sometimes plants yield more when planted together.
Grass, bushes, trees, and carrots yield more when they have the right plant companion. Companion preference is different for each individual plant and cannot be predicted. Fortunately, the companion preference of the plant under the drone can be measured using `game.get_companion()`. It returns a `Companion` — the type of plant it wants as its companion and the position where it wants its companion.

```gleam
case game.get_companion() {
  Some(companion) -> {
    echo companion.entity
    echo companion.position
    Nil
  }
  None -> Nil
}
```

For example if you plant a bush and then call `game.get_companion()` it will return something like `Companion(entity: game.Carrot, position: Position(x: 3, y: 5))`. This means that this bush would like to have carrots at the position `(3,5)`. So if you plant carrots at `(3,5)` and then harvest the bush, it will yield more wood. The growth stage of the carrot doesn't matter.

A plant's companion preference can be `game.Grass`, `game.Bush`, `game.Tree` or `game.Carrot`. Each plant chooses this randomly, but it will always choose a different plant than itself. The position can also be any position within 3 moves of the plant except the position of the plant itself.

If there is no plant under the drone that has a companion preference, `game.get_companion()` will return `None`.

Before polyculture is unlocked, the yield multiplier is `5`. It doubles every time you upgrade it.