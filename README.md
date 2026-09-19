# Lucy was Replaced

https://github.com/user-attachments/assets/f247f92b-9493-4e8b-a41b-d544b17fbb0f

Play *The Farmer Was Replaced* with the **Gleam** programming language instead of
the game's built-in Python.

## Why Gleam?

- **Fewer surprises**: Gleam checks your code before it runs, so you catch mistakes early.
- **Easy to read**: little syntax and one way to do things means code reads the same everywhere.
- **Beginner friendly**: if you can follow a recipe, you can write a Gleam program.

## What can you do with it?

- **Automate your farm** just like with Python: move, till, plant, and harvest.
- **Use the whole game toolkit**: sensors, items, unlocks, and the occasional flip.
- **Run several drones at once** to work the farm faster.
- **Split your code across windows** and reuse it like building blocks.

## Examples

A first program might look like this:

```gleam
import game

pub fn main() {
	game.harvest()
	game.do_a_flip()
	main()
}
```

...but a few minutes in you might end up with this:

```gleam
// utils.gleam
import game
import gleam/list

pub fn visit_all(size: Int, do_work) {
	use _ <- list.each(list.repeat(True, size))
	game.move(game.East)
	use _ <- list.each(list.repeat(True, size))
	game.move(game.North)
	do_work()
}

pub fn repeat(do_work: fn() -> Nil) {
	do_work()
	repeat(do_work)
}

pub fn try_harvest_with(func: fn() -> Bool) {
	case game.can_harvest() {
		True -> {
			game.harvest()
			func()
		}
		_ -> False
	}
}

pub fn try_harvest() {
	try_harvest_with(fn() { True })
}
```

```gleam
//farm_carrots.gleam
import game
import utils

pub fn main() {
	utils.visit_all(3, fn() {
		game.till()
		game.plant(game.Carrot)
	})
	
	use <- utils.repeat()
	use <- utils.visit_all(3)

	use <- utils.try_harvest_with()
	game.plant(game.Carrot)
}
```

## Installing

The easiest way is with **r2modman**:

1. Install [r2modman](https://thunderstore.io/c/the-farmer-was-replaced/p/ebkr/r2modman/)
   and pick *The Farmer Was Replaced* as the game.
2. In the mod browser, search for **Lucy Was Replaced** and install it.
   BepInEx is pulled in automatically.
3. Launch the game through r2modman and press **Run** in any code window.

Prefer to do it by hand? Follow the manual install steps in
[docs/INSTALL.md](docs/INSTALL.md) (also included in the package).
