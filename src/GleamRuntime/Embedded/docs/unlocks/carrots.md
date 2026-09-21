# Carrots

Before you can plant carrots with `game.plant(game.Carrot)`, you have to till the soil. This will change the ground to `game.Soil`. To till the soil, simply call `game.till()`. Calling `game.till()` again will change it back to `game.Grassland`.

```gleam
game.till()
game.plant(game.Carrot)
```

Planting carrots costs wood and hay. These items will be automatically removed when calling `game.plant(game.Carrot)`.

You can see the cost of any plant on its [own page](objects/carrot).