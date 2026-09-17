import game
import gleam/dynamic.{type Dynamic}
import gleam/dynamic/decode
import gleam/int
import gleam/io
import gleam/option.{None, Some}

// Spawn three drones, each harvesting the farm quadrant it is assigned to by
// its drone id, then wait for all of them to finish. Each worker must be a
// named `pub fn` in this module — spawned drones run it in their own engine.

pub fn main() {
  let handles = game.spawn_drone_with(3, worker)
  case handles {
    [a, b, c, ..] -> {
      await(a)
      await(b)
      await(c)
    }
    _ -> io.println("no drones spawned")
  }
}

pub fn worker() -> Dynamic {
  let me = game.get_drone_id()
  io.println("drone " <> int.to_string(me) <> " started")
  // A real worker would loop over its quadrant here (till / plant / harvest).
  // Simulated with a short countdown so the run completes:
  let _ = countdown(200)
  dynamic.int(me)
}

fn countdown(n: Int) -> Int {
  case n {
    0 -> 0
    _ -> countdown(n - 1)
  }
}

fn await(handle: game.DroneHandle) {
  case game.wait_for(handle) {
    Some(value) -> {
      case decode.run(value, decode.int) {
        Ok(id) -> io.println("drone " <> int.to_string(id) <> " finished")
        Error(_) -> io.println("worker result was not an int")
      }
    }
    None -> io.println("worker could not be awaited")
  }
}