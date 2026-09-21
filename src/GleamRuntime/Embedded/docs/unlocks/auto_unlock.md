# Auto Unlocks

To fully automate the game, you can use `unlock.unlock()` to automatically unlock features.

For example, you can use `unlock.unlock(unlock.Speed)` and `unlock.unlock(unlock.Expand)` to unlock the speed and expansion features.

```gleam
import game/unlock as unlock

let _ = unlock.unlock(unlock.Speed)
let _ = unlock.unlock(unlock.Expand)
```

To see what an unlock costs, open its page in the docs window — the current cost is shown at the bottom.

You can check how many times an unlock has been bought with `unlock.num_unlocked()`:

```gleam
unlock.num_unlocked(unlock.Speed) > 0
```