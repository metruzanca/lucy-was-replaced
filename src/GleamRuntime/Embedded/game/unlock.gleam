//! Game unlocks (the research tree).
//!
//! `import game/unlock` then `unlock.unlock(unlock.Megafarm)`.

pub type Unlock {
  AutoUnlock
  Cactus
  Carrots
  Costs
  Debug
  Debug2
  Dictionaries
  Dinosaurs
  Expand
  Fertilizer
  Functions
  Grass
  Hats
  Import
  Leaderboard
  Lists
  Loops
  Mazes
  Megafarm
  Operators
  Plant
  Polyculture
  Pumpkins
  Senses
  Simulation
  Speed
  Sunflowers
  TheFarmersRemains
  Timing
  TopHat
  Trees
  Utilities
  Variables
  Watering
}

fn unlock_name(unlock: Unlock) -> String {
  case unlock {
    AutoUnlock -> "auto_unlock"
    Cactus -> "cactus"
    Carrots -> "carrots"
    Costs -> "costs"
    Debug -> "debug"
    Debug2 -> "debug_2"
    Dictionaries -> "dictionaries"
    Dinosaurs -> "dinosaurs"
    Expand -> "expand"
    Fertilizer -> "fertilizer"
    Functions -> "functions"
    Grass -> "grass"
    Hats -> "hats"
    Import -> "import"
    Leaderboard -> "leaderboard"
    Lists -> "lists"
    Loops -> "loops"
    Mazes -> "mazes"
    Megafarm -> "megafarm"
    Operators -> "operators"
    Plant -> "plant"
    Polyculture -> "polyculture"
    Pumpkins -> "pumpkins"
    Senses -> "senses"
    Simulation -> "simulation"
    Speed -> "speed"
    Sunflowers -> "sunflowers"
    TheFarmersRemains -> "the_farmers_remains"
    Timing -> "timing"
    TopHat -> "top_hat"
    Trees -> "trees"
    Utilities -> "utilities"
    Variables -> "variables"
    Watering -> "watering"
  }
}

@external(javascript, "../game_ffi.mjs", "unlock_code")
fn unlock_code(name: String) -> Bool

/// Spend resources to unlock (or upgrade) a feature in the research tree, e.g.
/// `unlock.Megafarm` or `unlock.Carrots`. Has exactly the same effect as
/// clicking the button in the research tree.
pub fn unlock(unlock: Unlock) -> Bool {
  unlock_code(unlock_name(unlock))
}

@external(javascript, "../game_ffi.mjs", "num_unlocked_code")
fn num_unlocked_code(name: String) -> Int

/// How many times an unlock has been bought (`0` = not unlocked). For
/// upgradeable unlocks this is `1` plus the number of upgrades.
pub fn num_unlocked(unlock: Unlock) -> Int {
  num_unlocked_code(unlock_name(unlock))
}