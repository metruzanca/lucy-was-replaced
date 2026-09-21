# Simulation

Simulations allow you to quickly test code without changing the state of the real farm.

Simulations are started from the game's own Python scripts with `simulate()`. Gleam programs cannot start a simulation yet.

When you do use simulations, the starting state of the simulation can be chosen freely, and when the simulation ends, the real farm will be in the exact state it was in before the simulation started. The simulation runs the file you name, starting with the unlocks, items, globals, random seed and speedup you choose. The result is always deterministic for the same seed and starting conditions.