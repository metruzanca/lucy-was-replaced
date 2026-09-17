# Changelog

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