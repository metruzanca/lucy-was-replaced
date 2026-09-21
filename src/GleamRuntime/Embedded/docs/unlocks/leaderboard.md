# Leaderboard

If you have made it this far, you have overcome many challenges. But have you solved them efficiently?
You can compete with other players on various leaderboards for the most efficient farming methods.

Leaderboard runs are started from the game's own Python scripts with `leaderboard_run()`. Gleam programs cannot start a leaderboard run yet, but you can still prepare your code — simulations and leaderboards use the same farm the rest of your program uses.

To reduce variance, all runs are required to run for at least 2 hours (You can speed it up, so it won't take that long).

![](LeaderboardSetup400)

## Fastest Reset

The fastest reset is the most prestigious category. Completely automate the game from a single farm plot to unlocking the leaderboards again.

You do not have to unlock everything, just try to unlock `unlock.Leaderboard` as fast as possible.

Remember that you can use `unlock.num_unlocked(unlock.Leaderboard) > 0` to check if something is unlocked.

```gleam
import game/unlock as unlock

unlock.num_unlocked(unlock.Leaderboard) > 0
```

## Maze

Start with everything unlocked and farm `9863168` gold as fast as you can. This is exactly the amount of gold you will earn by reusing one 32x32 maze `300` times.

## Dinosaur

Start with everything unlocked and farm `33488928` bones as fast as you can. This is exactly the number of bones you will get if you fill a 32x32 area with the dinosaur tail.

## Other Resource Leaderboards

Each plant has its own leaderboard for farming that particular plant as quickly as possible. You start with all the unlocks, the resources you need to grow the plant, and lots of power. The goal is to farm a set amount of the resource produced by the plant.

## Single Drone Leaderboards

There are also Leaderboards for farming with a single drone. You only get one drone and an 8x8 farm and have to farm a certain amount of resources as quickly as possible.