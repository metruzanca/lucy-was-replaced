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
	try_harvest_with(fn() {})
}
```

```gleam
//farm_carrots.gleam
import game
import gleam/list
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

(Awaiting approval on thunderstore for easy installs via r2modman/similar)

Extract the zip into the game folder with BepInEx 5 installed. Detailed steps are in [docs/INSTALL.md](docs/INSTALL.md) (also included in the package).


- **Install BepInEx 5 x64.**
   Download the latest `BepInEx_win_x64_5.4.x.zip` from the
   [BepInEx releases](https://github.com/BepInEx/BepInEx/releases) page (the x64
   build, not x86).
   Extract it **into the game folder** so that `winhttp.dll` and the `BepInEx/`
   folder sit right next to `TheFarmerWasReplaced.exe`:
2. **Install the mod.**
   Extract `GleamFarmer-<version>.zip` into the game folder and merge the
   `BepInEx/` folder when prompted. You should end up with:

   ```
   BepInEx/plugins/GleamFarmer/
     GleamFarmer.dll
     GleamRuntime.dll
     Jint.dll
     … (other DLLs)
     Embedded/   (compiler wasm, Gleam stdlib, game module)
     INSTALL.md
     THIRD_PARTY_NOTICES.md
   ```

3. **Launch the game.** The BepInEx console/log (`BepInEx/LogOutput.log`) should
   show `Lucy was Replaced <version> loaded.`
4. **Run Gleam.** In any code window, write a program and press **Run**:

   ```gleam
   import game

   pub fn main() {
     game.harvest()
     game.do_a_flip()
     main()
   }
   ```
