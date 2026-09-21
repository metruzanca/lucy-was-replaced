# Mega Farm

This incredibly powerful unlock gives you access to multiple drones.

As before, you still start with just one drone. Additional drones must first be spawned and will disappear after the program terminates.
Each drone runs its own separate program. New drones can be spawned with `game.spawn_drone(count, worker)`, which spawns `count` drones running `worker`. The worker must be a `pub fn` in your module.

```gleam
import game
import gleam/list

pub fn worker() {
  use _ <- list.each(list.repeat(True, game.get_world_size()))
  game.harvest()
  game.move(game.North)
}

pub fn main() {
  let handles = game.spawn_drone(3, worker)
  case handles {
    [first, ..] -> game.wait_for(first)
    _ -> Nil
  }
}
```

This spawns new drones in the same position as the drone that ran `game.spawn_drone()`. Each new drone then begins executing `worker`. After it is done, it will disappear automatically.

Drones do not collide with each other.

Use `game.max_drones()` to get the maximum number of drones that can exist simultaneously.
Use `game.num_drones()` to get the number of drones that are already on the farm.

## All Drones Are Equal

There is no special "main" drone. All drones can spawn other drones, and they all count toward the drone limit. All drones disappear when they terminate. If the first drone finishes its program early, another drone will become the one whose execution is visualized with code highlights. All drones can trigger breakpoints, and when a drone triggers a breakpoint, the code highlighting switches to that drone.

## Awaiting Another Drone

Use `game.wait_for(handle)` to wait for a drone to finish. You receive the `handle` when you spawn the drone.
`game.wait_for(handle)` returns the drone's result if it returned one.

```gleam
let handles = game.spawn_drone_with(1, worker)
case handles {
  [handle, ..] -> game.wait_for(handle)
  _ -> None
}
```

Note that spawning drones takes time, so it's not a good idea to spawn a new drone for every little thing.

You can use `game.has_finished(handle)` to check if a drone has finished without waiting for it.

## No Shared Memory

Each drone has its own memory and cannot directly read or write another drone's values.

## Talking To Each Other

Drones can exchange messages. `game.send(message, drone_id)` sends a value to a drone's mailbox; `game.receive()` picks up the next message (non-blocking). `game.get_drone_id()` tells a drone its own id.

```gleam
game.send("pick a row", 2)
```

## Race Conditions

Multiple drones can interact with the same farm tile at the same time. If two drones interact with the same tile during the same tick, both interactions will occur, but the results may differ based on the order of the interactions.

For example, imagine that drones `0` and `1` are both over the same tree that is almost fully grown.
Drone `0` calls `item.use_item(item.Fertilizer)` and drone `1` calls `game.harvest()`.

If these actions occur at the same time, the tree will first be fertilized and then harvested. In that case, you will receive wood from it. However, if Drone `1` is slightly faster, the tree will be harvested before it is fertilized, and you will not receive the wood.
This is called a "race condition." It is a common issue in parallel programming, where the result depends on the order in which operations are performed.

Here's another problematic situation that can happen when multiple drones run the same code simultaneously at the same position.

```gleam
case game.get_water() <. 0.5 {
  True -> {
    let _ = item.use_item(item.Water)
    Nil
  }
  False -> Nil
}
```

If multiple drones run this simultaneously, they will all run the first line, which puts them into the `if` block. Then, they will all use water, wasting a lot of it.
By the time a drone reaches the `item.use_item` call, `game.get_water()` might no longer be less than `0.5` because another drone has watered the tile in the meantime.