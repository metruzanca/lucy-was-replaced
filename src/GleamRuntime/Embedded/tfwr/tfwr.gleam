//! GleamFarmer FFI bridge to The Farmer Was Replaced.
//!
//! Full game implementations land in M4; these typed stubs exercise the
//! @external path end to end and log their calls.

@external(javascript, "./tfwr_ffi.mjs", "harvest")
pub fn harvest() -> Nil

@external(javascript, "./tfwr_ffi.mjs", "move")
pub fn move(direction: Int) -> Nil

@external(javascript, "./tfwr_ffi.mjs", "get_pos_x")
pub fn get_pos_x() -> Int

@external(javascript, "./tfwr_ffi.mjs", "get_pos_y")
pub fn get_pos_y() -> Int

/// Direction constants. The JS side maps these to the game's own values.
pub const north = 0
pub const east = 1
pub const south = 2
pub const west = 3