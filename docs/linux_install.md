# Linux / Steam Deck (Proton) Install

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
