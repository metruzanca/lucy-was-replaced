// GleamFarmer game FFI. These thin wrappers call into the plugin's C# host,
// registered in the Jint engine as the `__gleam_host` global. All values are
// primitives; the Gleam modules map the custom types (Direction/Entity/Item)
// to the integer codes via pattern matching.
export function move_code(direction) { return __gleam_host.move(direction); }
export function can_move_code(direction) { return __gleam_host.can_move(direction); }
export function harvest() { return __gleam_host.harvest(); }
export function can_harvest() { return __gleam_host.can_harvest(); }
export function plant_code(entity) { return __gleam_host.plant(entity); }
export function till() { return __gleam_host.till(); }
export function swap_code(direction) { return __gleam_host.swap(direction); }
export function clear() { return __gleam_host.clear(); }
export function get_pos_x() { return __gleam_host.get_pos_x(); }
export function get_pos_y() { return __gleam_host.get_pos_y(); }
export function get_world_size() { return __gleam_host.get_world_size(); }
export function get_entity_type_code() { return __gleam_host.get_entity_type_code(); }
export function get_ground_type_code() { return __gleam_host.get_ground_type_code(); }
export function get_water() { return __gleam_host.get_water(); }
export function num_items_code(item) { return __gleam_host.num_items(item); }
export function use_item_code(item, count) { return __gleam_host.use_item(item, count); }
export function get_time() { return __gleam_host.get_time(); }
export function get_tick_count() { return __gleam_host.get_tick_count(); }
export function measure() { const v = __gleam_host.measure(); return v === null || v === undefined ? null : Array.from(v); }
export function measure_at_code(direction) { const v = __gleam_host.measure_at(direction); return v === null || v === undefined ? null : Array.from(v); }
export function get_companion() { const v = __gleam_host.get_companion(); return v === null ? null : Array.from(v); }
export function get_cost_code(entity) { return Array.from(__gleam_host.get_cost(entity)); }
export function random() { return __gleam_host.random(); }
export function num_drones() { return __gleam_host.num_drones(); }
export function max_drones() { return __gleam_host.max_drones(); }
export function unlock_code(name) { return __gleam_host.unlock(name); }
export function num_unlocked_code(name) { return __gleam_host.num_unlocked(name); }
export function set_execution_speed(speed) { return __gleam_host.set_execution_speed(speed); }
export function set_world_size(size) { return __gleam_host.set_world_size(size); }
export function do_a_flip() { return __gleam_host.do_a_flip(); }
export function pet_the_piggy() { return __gleam_host.pet_the_piggy(); }
export function change_hat(name) { return __gleam_host.change_hat(name); }
export function quick_print(text) { return __gleam_host.quick_print(text); }

// ---- drones ----

export function spawn_drone_code(count, worker) {
  if (typeof worker !== "function" || !worker.name) {
    throw new Error("spawn_drone: the worker must be a named `pub fn` (anonymous functions cannot be spawned)");
  }
  return Array.from(__gleam_drones.spawn_drone(count, worker.name));
}
export function get_drone_id() { return __gleam_drones.get_drone_id(); }
export function wait_for_code(id, generation) {
  const s = __gleam_drones.wait_for(id, generation);
  return s === null || s === undefined ? null : JSON.parse(s);
}
export function has_finished_code(id, generation) { return __gleam_drones.has_finished(id, generation); }
export function send_code(message, toDroneId) {
  __gleam_drones.send(JSON.stringify(message === undefined ? null : message), toDroneId);
}
export function receive_code(fromDroneId) {
  const s = __gleam_drones.receive(fromDroneId);
  return s === null || s === undefined ? null : JSON.parse(s);
}