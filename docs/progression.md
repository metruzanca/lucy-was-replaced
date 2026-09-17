# Game Progression

Two views of *The Farmer Was Replaced* progression: the **vanilla** game (reference) and the
**Gleam-first** re-scoping, which folds the language progression into the world progression so
Gleam concepts are introduced stage-by-stage the way Python syntax is today.

## 1. Vanilla progression (reference)

### How unlocking works
- `unlock(tech)` buys a tech with items; `get_cost(tech)` reads the price; `num_unlocked(tech)`
  counts purchases (`0` = not owned). Prices scale with each purchase.
- Unlocks come in three kinds:

| Kind | Examples | Behaviour |
|---|---|---|
| **Language / tooling** | `Variables`, `Operators`, `Loops`, `Functions`, `Lists`, `Dictionaries`, `Utilities`, `Import`, `Senses`, `Costs`, `Debug`, `Debug_2`, `Timing`, `Simulation`, `Auto_Unlock` | One-time. Buying `Functions` lets you write `def`. Nothing to upgrade. |
| **Content / upgrade** | `Plant`, `Grass`, `Trees`, `Carrots`, `Pumpkins`, `Sunflowers`, `Cactus`, `Dinosaurs`, `Mazes`, `Expand`, `Watering`, `Fertilizer`, `Speed`, `Megafarm`, `Polyculture`, `Leaderboard` | Repeatable; first purchase unlocks, later purchases upgrade yield/cost. |
| **Cosmetic** | `Hats` and friends | `change_hat()` fluff. |

- Language unlocks are **soft gates**: they gate the Python autocomplete, docs visibility, and a
  flashing "unlock loops" hint — they do not block the parser. Content unlocks gate world actions
  (e.g. `plant` asserts the crop's unlock).

### The vanilla stages

| Stage | World unlocks | Language/tooling introduced |
|---|---|---|
| **0 · One tile** | harvest grass → hay; buy `Plant`, `Senses` | `Variables`, `Loops` (the flashy first unlock), `Functions` |
| **1 · Movement & soil** | `Expand` (movement), `Carrots` (till + plant), `Watering` | — |
| **2 · Wood & speed** | `Trees`, `Grass` (yield), `Speed` | `Lists`, `Utilities` (`min/max/abs/random`), `Costs` |
| **3 · Pumpkins & fertilizer** | `Pumpkins` (fusion), `Fertilizer` | — |
| **4 · Megafarm** | `Megafarm` (multi-drone) | `Import`, drone functions |
| **5 · Non-linear crops** | `Cactus` (sorting), `Sunflowers` (power), `Polyculture` (companions) | `Dictionaries` / `Sets` |
| **6 · Mazes & dinosaurs** | `Mazes`, `Dinosaurs` | — |
| **7 · Leaderboard** | `Leaderboard` | `Timing`, `Debug_2`, `Simulation`, `Auto_Unlock` |

Canonical minimum path (community reference run, 32 purchases): `Speed` ×5, `Fertilizer` ×4,
`Carrots`/`Watering`/`Trees`/`Expand` ×3, `Pumpkins`/`Grass`/`Cactus`/`Dinosaurs` ×2,
`Plant`/`Mazes`/`Leaderboard` ×1 — alternating capability and throughput. Reset runs skip all
language tech (it does not affect a run).

## 2. Suggested changes (Gleam mode)

### Principle
The **world** progression stays the backbone — crops, soil, water, speed, expand, multi-drone,
leaderboard. The **language** progression is re-scoped from "Python syntax unlocks" into a
**Gleam curriculum** that runs *alongside* the world stages: each stage also introduces the next
piece of Gleam.

Gleam is always fully compilable (our runtime never hard-gates syntax), so the curriculum is
**pedagogical, not enforced**: each stage's docs/examples surface the next concept. Some concepts
are foundational (functions, `case`); others are "optional but prettier" (`|>` pipelines, `use`)
and are deliberately held back until the player automates multi-step tasks.

### Auto-granted in Gleam mode
All vanilla language/tooling unlocks are granted from the start (in-memory, on every save load):
- nothing is ever shown as gated or hinted (no "unlock loops" flash);
- the Python autocomplete is irrelevant (Gleam code never uses it).

Their nodes are hidden from the research tree (see *Mechanism* below), so the tree presents only
the world economy.

### The Gleam curriculum (mapped onto world stages)

| Stage | World unlocks | Gleam concepts introduced |
|---|---|---|
| **0 · One tile** | harvest, `Plant`, `Senses` | `pub fn main()`, `import gleam/io`, `import game`, `io.println`, `let` bindings, integers/strings & `<>`, `case`, `game.harvest()` / `game.can_harvest()` / `game.get_entity_type()` |
| **1 · Movement & soil** | `Expand`, `Carrots`, `Watering` | `game.move/till/plant`, **functions with parameters**, **recursion** (replaces "loops"), `case` on custom types (`game.Ground`, `game.Entity`), tuples `#(…)`, `game.get_pos()` |
| **2 · Wood & speed** | `Trees`, `Grass`, `Speed` | lists `[1,2,3]`, `list.length/map/fold`, `game/item.num_items`, `int.to_string`, comparisons |
| **3 · Pumpkins & fertilizer** | `Pumpkins`, `Fertilizer` | higher-order functions (`fn` args), `Result`/`Option`, **pipelines `\|>`**, `string.join` |
| **4 · Megafarm** | `Megafarm`, drone funcs | **`use` expressions**, labelled arguments, multi-module `import` |
| **5 · Non-linear crops** | `Cactus`, `Sunflowers`, `Polyculture` | **records / custom types** for state, `gleam/dict` |
| **6 · Mazes & dinosaurs** | `Mazes`, `Dinosaurs` | tail recursion, `Result` error handling, list algorithms, `panic` / `todo` |
| **7 · Leaderboard** | `Leaderboard` | `game.get_time` / `game.get_tick_count`, `set_execution_speed`, optimisation patterns |

### Vanilla language unlock → Gleam mapping

| Vanilla unlock | Gleam counterpart | Introduced |
|---|---|---|
| `Loops` / `while` / `for` / `break` / `continue` | **Recursion** (no loops) | Stage 1 |
| `Variables` | `let` bindings | Stage 0 |
| `Functions` / `def` / `return` | `fn` / return | Stage 0 (recursion Stage 1) |
| `Operators` | Gleam operators | Stage 0 |
| `Lists` | `gleam/list` | Stage 2 |
| `Dictionaries` / `Sets` | records + `gleam/dict` | Stage 5 |
| `Utilities` (`min/max/abs/random`) | `gleam/int`, `gleam/float`, `gleam/random` | Stage 2 |
| `Senses` | `game.get_*` sensors | Stage 0–1 |
| `Costs` | `game.get_cost` (declared; bridge pending) | Stage 2 |
| `Import` | `import` (multi-module) | Stage 0 (multi Stage 4) |
| `Timing` | `game.get_time` / `get_tick_count` | Stage 7 |
| `Debug` / `Debug_2` | `io.println`, `set_execution_speed` | Stage 0 / 7 |
| `Simulation` | `game.simulate` (planned) | Stage 7 |
| `Auto_Unlock` | `game.unlock` / `game.num_unlocked` | Stage 2 |

### Optional Gleam syntax, deliberately held back

| Syntax | Why later | Introduced |
|---|---|---|
| `\|>` pipelines | reads naturally once steps compose | Stage 3 |
| `use` | shines with sequential effects / drone ops | Stage 4 |
| labelled arguments | ergonomics after real multi-arg functions | Stage 4 |
| full pattern matching (records / lists) | power worth earning | Stage 5 |
| `panic` / `todo` / `let assert` | after `Result` / `Option` | Stage 5–6 |

### Mechanism
- **Code:** `src/Plugin/Patches/ProgressionPatches.cs`
  - `AutoGrantUnlocksPatch` — `MainSim.SetupSim` postfix: grants every language/tooling unlock
    (expanding both the unlock name and its `unlocks` entries) whenever a sim is set up.
  - `ResearchTreePatch` — `ResearchMenu.Setup` prefix: sets `UnlockSO.enabled = false` for the
    language/tooling unlocks in Gleam mode (re-enabled when Gleam mode is off; re-derived on every
    `Setup`, so it self-heals).
  - The hidden set is **data-driven**: curated names ∪ any unlock whose `unlocks` gates a keyword
    (`Farm.allKeyWords`). A one-time log reports the hidden set and flags if any *visible* node
    references a hidden unlock as its parent (orphan risk).
- Both patches are no-ops unless `GleamHost.Enabled` (Gleam mode).
- The curriculum itself lives in `docs/curriculum.md` and `examples/stage0…stage7.gleam`.

### World economy
Unchanged: `plant` stays gated by crop unlocks; `Speed`/`Expand`/`Megafarm` remain the purchase
economy. Only language/tooling unlocks are auto-granted/hidden.

### Open questions
1. Should a handful of language nodes be **re-purposed as visible Gleam concept nodes** (in-tree
   curriculum) rather than hidden, once the tree layout is confirmed safe?
2. Where should the Gleam docs live — in-game doc windows or bundled markdown?