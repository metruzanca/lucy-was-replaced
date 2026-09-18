# Lucy was Replaced


https://github.com/user-attachments/assets/f247f92b-9493-4e8b-a41b-d544b17fbb0f

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
