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
export function get_pos_x() { return __gleam_host.get_pos_x(); }
export function get_pos_y() { return __gleam_host.get_pos_y(); }
export function get_world_size() { return __gleam_host.get_world_size(); }
export function get_entity_type_code() { return __gleam_host.get_entity_type_code(); }
export function get_ground_type_code() { return __gleam_host.get_ground_type_code(); }
export function get_water() { return __gleam_host.get_water(); }
export function num_items_code(item) { return __gleam_host.num_items(item); }
export function use_item_code(item) { return __gleam_host.use_item(item); }
export function get_time() { return __gleam_host.get_time(); }
export function get_tick_count() { return __gleam_host.get_tick_count(); }