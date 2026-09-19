# Manual Install

Install Lucy was Replaced without a mod manager. You'll need a download of the
`GleamFarmer-<version>.zip` package.

1. **Install BepInEx 5 x64.**

   Download the latest `BepInEx_win_x64_5.4.x.zip` from the
   [BepInEx releases](https://github.com/BepInEx/BepInEx/releases) page (the x64
   build, not x86). Extract it **into the game folder** so that `winhttp.dll`
   and the `BepInEx/` folder sit right next to `TheFarmerWasReplaced.exe`.

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

## Linux / Steam Deck (Proton)

1. Install the **Windows** BepInEx 5 x64 build into the game folder, as in
   step 1 above.
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
4. Launch and press **Run** as in step 4 above.