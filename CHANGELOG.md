# Changelog

## Unreleased

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