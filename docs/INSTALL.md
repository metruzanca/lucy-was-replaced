# Installing Lucy was Replaced

Write **Gleam** instead of Python in *The Farmer Was Replaced*.

## Requirements

- *The Farmer Was Replaced* (Steam, app id 2060160)
- **BepInEx 5 (x64)**. The mod runs as a BepInEx plugin and depends on the
  `BepInEx-BepInExPack-5.4.2305` package. Install it with a mod manager
  (automatic) or by hand (see below).

## Option A — Mod manager (recommended)

r2modman, Gale, or the Thunderstore Mod Manager handle both BepInEx and this mod
for you.

1. Install a mod manager and open it.
2. Pick **The Farmer Was Replaced** as the game.
3. On the [Thunderstore page](https://thunderstore.io/c/the-farmer-was-replaced/)
   click **Install with Mod Manager** for *Lucy_Was_Replaced* (or find it inside
   the manager's browser).
4. The manager reads the dependency string and installs **BepInExPack** (BepInEx 5)
   automatically.
5. Launch the game **through the mod manager**, open a save, and press **Run**.

## Option B — Manual install (Windows)

1. **Install BepInEx 5 x64.**
   Download the latest `BepInEx_win_x64_5.4.x.zip` from the
   [BepInEx releases](https://github.com/BepInEx/BepInEx/releases) page (the x64
   build, not x86).
   Extract it **into the game folder** so that `winhttp.dll` and the `BepInEx/`
   folder sit right next to `TheFarmerWasReplaced.exe`:

   ```
   TheFarmerWasReplaced.exe
   winhttp.dll
   BepInEx/
   ```

   (If you already have another BepInEx mod installed, skip this step.)

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
   import gleam/io

   pub fn main() {
     game.till()
     game.plant(game.Carrot)
     io.println("planted a carrot")
   }
   ```

## Linux / Steam Deck (Proton)

1. Install the **Windows** BepInEx 5 x64 build into the game folder, as in
   Option B above.
2. Wine prefers its built-in `winhttp`, so force the game to use BepInEx's proxy.
   With the game **closed**, either set the Steam launch option:

   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```

   or add the override to the prefix once:

   ```sh
   WINEPREFIX="$HOME/.local/share/Steam/steamapps/compatdata/2060160/pfx" \
     wine reg add 'HKCU\Software\Wine\AppDefaults\TheFarmerWasReplaced.exe\DllOverrides' \
     /v winhttp /d "native,builtin" /f
   ```

3. Extract the `GleamFarmer-<version>.zip` into the game folder.
4. Launch and press **Run** as above.

## Notes

- Output and errors go to `BepInEx/LogOutput.log`; `io.println` also shows the
  game's floating print bubbles above the drone.
- The Run/Execute button toggles: press again to stop a running program.
- The editor highlights Gleam syntax; the game's Python built-ins do not apply.
- To edit code outside the game, edit `main.py` in the save directory while the
  game is **closed**, then launch (the game's file watcher reloads it).
- Other code windows are importable Gleam modules; helpers you call across windows
  need `pub fn`.

## Uninstalling

Delete `BepInEx/plugins/GleamFarmer/`. (Remove BepInEx too if you no longer want
any mods.)