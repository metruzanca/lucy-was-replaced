# Fertilizer

At some point, waiting for the plants to grow is just not efficient enough anymore.
Similar to water, you will automatically receive 1 fertilizer every 10 seconds, doubling with each upgrade.

Fertilizer can make plants grow instantly. `item.use_item(item.Fertilizer)` reduces the remaining growing time of the plant under the drone by 2 seconds.

```gleam
import game/item as item

let _ = item.use_item(item.Fertilizer)
```