# GleamFarmer repo conventions

## New `.gleam` files

When creating a new Gleam file (examples, scratch, snippets), start from the
repo template (`examples/template.gleam`):

```gleam
import game

pub fn main() {

}
```

## Common commands

- Build: `dotnet build GleamFarmer.sln` (inside `nix-shell`)
- Tests: `dotnet test tests/GleamRuntime.Tests`
- Run a Gleam file headless: `./scripts/run-gleam.sh examples/<name>.gleam`
- Hot-reload into the game: `./scripts/push-to-game-save.sh examples/<name>.gleam gleam`