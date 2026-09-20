# Changelog

## Unreleased

- Change your drone's hat with named constants instead of strings: `game.change_hat(game.StrawHat)` — and 25 more hats to pick from, from the humble traffic cone to the golden trophies.
- Unlock things in the research tree with named constants too: `import game/unlock` and `unlock.unlock(unlock.Megafarm)` — all 34 unlocks (crops, speed, the megafarm, even 'The Farmers Remains') are now typed, no more guessing strings like `"multi_trade"`.
- The in-game "First Program" page now teaches the Gleam way to start, beginning with the `import game` / `pub fn main()` skeleton instead of the old Python commands.

## 0.3.1

- A clear disclosure that this mod is built with heavy help from AI coding
  agents. It's on the mod page and embedded in the package itself, so it's
  always visible how the mod was made.

## 0.3.0

- A new color theme: switch to **Gleam** under Settings → color theme and the editor is
  tinted with the Gleam brand — pink functions, blue records and types, yellow strings,
  and a deep navy background.
- Watch your program run: the editor highlights the line currently executing, just like
  the game's own Python editor. It moves fast in normal runs — that's the point.
- Slow things down and really read your code: the step-by-step button now works for
  Gleam too. Start a run in step mode and press it once per line to walk through your
  program one statement at a time.

## 0.2.0

- The in-game help now speaks Gleam: the docs window, hover tooltips, and autocomplete
  show the `game.*` functions you actually write, plus a quick tour of the Gleam language
  and the built-in helpers.
- Edit your code in your favorite editor: every code window is mirrored to a real Gleam
  project on disk, so you get full autocomplete, hover, and error checking from VS Code,
  Neovim, or any editor with Gleam support. Create, rename, or delete a window and the
  files follow — and vice versa.
- `echo` now prints to the game's output panel, just like `quick_print`.
- The editor no longer suggests Python keywords — it's Gleam from the start.
- A clearer error when you run an empty program, and a more helpful first program to
  start from.
- Installing is simpler: r2modman is now the recommended way, with a separate guide for
  manual installs.
- The in-game help screen links to the GitHub project — use Discussions for feedback and
  ideas, and Issues to report bugs.

## 0.1.0

Initial release.

- Run real **Gleam** in The Farmer Was Replaced's editor — the actual Gleam compiler
  (embedded as WASM) compiles your code to JavaScript, executed in an in-process engine.
- Typed `game` module: movement, farming, sensors, utilities, cosmetics — actions paced
  like the game's own interpreter (ops × OpDuration).
- Full tick-engine parity: computation is op-accounted too (`get_tick_count`, `get_time`,
  power drain), paced at the game's tick rate.
- **Multi-drone**: `spawn_drone` / `get_drone_id` / `wait_for` / `has_finished` / `send` /
  `receive` — spawned drones run concurrently, one engine per drone, sharing the tick budget.
- **Multi-module**: every open code window is an importable Gleam module (cross-module
  helpers need `pub fn`).
- Official (Steam) leaderboards are disabled, as required of all mods.