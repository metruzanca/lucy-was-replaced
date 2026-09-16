# Installing GleamFarmer

Write **Gleam** instead of Python in *The Farmer Was Replaced*.

## Requirements

- *The Farmer Was Replaced* (Steam, app id 2060160)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) **x64** for your platform

## Windows

1. Install BepInEx 5 x64 into the game folder:
   extract `BepInEx_win_x64_*.zip` so that `winhttp.dll` and `BepInEx/` sit next to
   `TheFarmerWasReplaced.exe`.
2. Extract `GleamFarmer-<version>.zip` into the game folder (merge `BepInEx/`).
   You should end up with `BepInEx/plugins/GleamFarmer/GleamFarmer.dll` and a
   `GleamFarmer/Embedded/` folder beside it.
3. Launch the game. The BepInEx console/log should show
   `GleamFarmer runtime ready (19 stdlib modules)`.
4. In a code window, write Gleam and press **Run**:

   ```gleam
   import game
   import gleam/io

   pub fn main() {
     game.till()
     game.plant(game.Carrot)
     io.println("planted a carrot")
   }
   ```

## Linux / Steam Deck (Proton)

1. Install the **Windows** BepInEx 5 x64 build into the game folder as above.
2. Wine prefers its built-in `winhttp`, so force the game to use BepInEx's proxy.
   With the game **closed**, either set the Steam launch option:

   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```

   or add the prefix override once:

   ```sh
   WINEPREFIX="$HOME/.local/share/Steam/steamapps/compatdata/2060160/pfx" \
     wine reg add 'HKCU\Software\Wine\AppDefaults\TheFarmerWasReplaced.exe\DllOverrides' \
     /v winhttp /d "native,builtin" /f
   ```

3. Extract the GleamFarmer zip into the game folder.
4. Launch and press **Run** as above.

## Notes

- Output and errors go to `BepInEx/LogOutput.log`; `io.println` also shows the
  game's floating print bubbles above the drone.
- The Run/Execute button toggles: press again to stop a running program.
- The editor highlights Gleam syntax; the Python built-ins do not apply.
- To edit code outside the game, edit `main.py` in the save directory while the
  game is **closed**, then launch (the game's file watcher reloads it).

## Uninstalling

Delete `BepInEx/plugins/GleamFarmer/`. (Remove BepInEx too if you no longer want
any mods.)
