# Lucy was Replaced

<video src="assets/readme_demo.mp4" controls></video>

Play *The Farmer Was Replaced* with **Gleam**, a real programming language, instead of
the game's built-in Python.

You type your farm code in the same editor you already know. The difference: your code is
written in Gleam, a friendly language that catches mistakes before they happen. It runs
right inside the game, paced exactly like the game's own scripts.

## Why Gleam?

- **Fewer surprises**: Gleam checks your code before it runs, so you catch mistakes early.
- **Easy to read**: clean formatting and a simple, consistent structure.
- **Beginner friendly**: if you can follow a recipe, you can write a Gleam program.

## What can you do with it?

- **Automate your farm** just like with Python: move, till, plant, and harvest.
- **Use the whole game toolkit**: sensors, items, unlocks, and the occasional flip.
- **Run several drones at once** to work the farm faster.
- **Split your code across windows** and reuse it like building blocks.

## A first program

```gleam
import game

pub fn main() {
	game.harvest()
	game.do_a_flip()
	main()
}
```

Press **Run**. Your drone tries to harvest, does a flip, and does it again. Press
**Run** again to stop.

## Installing

Two easy ways:

- **Mod manager (recommended)**: install r2modman or Gale, pick *The Farmer Was Replaced*,
  and click *Install with Mod Manager*. BepInEx is set up for you automatically.
- **By hand**: extract the zip into the game folder with BepInEx 5 installed. Detailed
  steps are in [docs/INSTALL.md](docs/INSTALL.md) (also included in the package).

## Learn more

- Everything you can do with the game API: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)
- Full install guide: [docs/INSTALL.md](docs/INSTALL.md)
- Report issues or contribute:
  [github.com/metruzanca/lucy-was-replaced](https://github.com/metruzanca/lucy-was-replaced)