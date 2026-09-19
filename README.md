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
- **Edit with an external editor**: your code windows are mirrored to a real Gleam
  project on disk, so you get full autocomplete, hover, and error checking from the
  Gleam language server. See [Editing from an external editor](#editing-from-an-external-editor).

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

## Feedback & roadmap

This mod is brand new and still taking shape — your feedback decides what comes next.

- **[GitHub Discussions](https://github.com/metruzanca/lucy-was-replaced/discussions)** — the best
  place for feedback, ideas, and questions. This is the primary channel.
- **[GitHub Issues](https://github.com/metruzanca/lucy-was-replaced/issues)** — for reporting bugs
  (the bug report form asks a few quick questions).

### Roadmap

Right now the focus is collecting feedback from early players. Planned next:

- **Auto-format your code on Run** — no more fighting the editor's indent keys.
- **Beginner-friendly teaching content** for writing Gleam in-game.
- **A custom leaderboard** so modded runs still have a way to be ranked.
- **Polish and testing** of multi-drone, the in-game docs, and external-editor workflows.
- **Anything players ask for** — say it in Discussions and it goes on the list.

## Installing

The easiest way is with **r2modman**:

1. Install [r2modman](https://thunderstore.io/c/the-farmer-was-replaced/p/ebkr/r2modman/)
   and pick *The Farmer Was Replaced* as the game.
2. In the mod browser, search for **Lucy Was Replaced** and install it.
   BepInEx is pulled in automatically.
3. Launch the game through r2modman and press **Run** in any code window.

Prefer to do it by hand? Follow the manual install steps in
[docs/INSTALL.md](docs/INSTALL.md) (also included in the package).

## Editing from an external editor

The in-game editor is nice, but for bigger programs you might want your usual
tools. The mod keeps every code window in sync with a **real Gleam project** on
disk, so you can open it in VS Code (Gleam extension), Neovim, or any editor with
Gleam LSP support, and get autocomplete, hover, go-to-definition, and error
checking against the actual `game.*` API.

Where is it? In the save folder (see [Where are the saves?](#where-are-the-saves)
for the exact path on your OS):

```
<save>/gleam-project/
├── gleam.toml        # target = "javascript", pins gleam_stdlib
├── manifest.toml     # dependency lock (same version the mod embeds)
└── src/
    ├── main.gleam    # one .gleam per code window (window name = module name)
    ├── utils.gleam
    ├── game.gleam        # LSP stub of the game API (read-only)
    ├── game/item.gleam
    └── game_ffi.mjs      # external glue for the stub
```

The mod creates this folder automatically on first Run. The game only watches
top-level `.py` files, so the `gleam-project` subfolder is left alone.

How it stays in sync:

- **In-game edits** are written to the project on Run and on every game save.
- **External edits** are picked up by a file watcher and pushed into the open
  code window.
- **Created/deleted/renamed files** create, close, or rename the matching code
  window (and closing or renaming a window updates the project).
- **Compiling reads the project**, so what the editor shows and what the game
  runs are the same bytes.

## Where are the saves?

The game stores each save in its own folder under `<persistent data>/Saves/`, one
per in-game save name (e.g. `Save0`, `Gleam`). The Gleam project the mod creates
lives in the save folder: `<save>/gleam-project/`.

| OS | Save folder |
|---|---|
| **Windows** | `%USERPROFILE%\AppData\LocalLow\TheFarmerWasReplaced\TheFarmerWasReplaced\Saves` |
| **macOS** | `~/Library/Application Support/com.TheFarmerWasReplaced.TheFarmerWasReplaced/Saves` |
| **Linux** (Proton) | `~/.local/share/Steam/steamapps/compatdata/2060160/pfx/drive_c/users/steamuser/AppData/LocalLow/TheFarmerWasReplaced/TheFarmerWasReplaced/Saves` |

So on Windows your `main` window's project would be at:

```
%USERPROFILE%\AppData\LocalLow\TheFarmerWasReplaced\TheFarmerWasReplaced\Saves\Save0\gleam-project\
```

Quick way to open it on Windows: press **Win + R**, paste the path, press Enter.

On Linux you can point the included script at a specific save by setting
`TFWR_SAVE_DIR` (see `scripts/push-to-game-save.sh`).

To get the language server working:

1. Install the Gleam CLI (`gleam` on [gleam.run](https://gleam.run) or your
   package manager) and the Gleam extension for your editor.
2. Open the `<save>/gleam-project/` folder as your workspace.
3. Run `gleam deps download` once (fetches `gleam_stdlib`, the same version the
   mod embeds).
4. Edit `.gleam` files, save, and press **Run** in the game.

Notes:

- Window names are module names: lowercase letters, digits and underscores only
  (`main`, `utils`, `farm_helper2`). Windows with invalid names aren't mirrored
  (the game's Python would refuse to import them too).
- **Create/delete/rename work both ways.** Create a `.gleam` file and it opens as
  a code window; delete it and the window closes; rename it and the window follows.
  Closing a window in-game deletes its `.gleam`, renaming one renames the file.
  Files created while the game is closed appear as windows on the next load.
- Toggle the feature off with the `ExternalProject` setting in the BepInEx
  config if you only want the in-game editor.
