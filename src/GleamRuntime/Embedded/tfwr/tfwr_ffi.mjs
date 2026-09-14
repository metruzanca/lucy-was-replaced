// GleamFarmer FFI stubs. M4 replaces these with calls into the game (via Jint
// host functions) so the drone actually acts in the world.
export function harvest() {
  console.log("[tfwr] harvest()");
}

export function move(direction) {
  console.log("[tfwr] move(" + direction + ")");
}

export function get_pos_x() {
  return 0;
}

export function get_pos_y() {
  return 0;
}