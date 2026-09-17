using System.Collections.Generic;

namespace GleamRuntime
{
    /// <summary>
    /// Host-side game API, called from JS (via `game_ffi.mjs` → `__gleam_host`).
    ///
    /// Integer codes: Direction North=0/East=1/South=2/West=3 · Entity
    /// 0=none,1=Grass..9=Hedge · Item 1=Hay..9=Fertilizer · Ground 1=Soil,2=Grassland.
    ///
    /// Implementations: <see cref="StubGameBridge"/> (headless/tests) and the
    /// plugin's real bridge (paced, main-thread dispatched).
    /// </summary>
    public interface IGameBridge
    {
        // Method names match the JS calls exactly (Jint's CLR interop is
        // case-insensitive but does not strip underscores).
        bool move(int direction);
        bool can_move(int direction);
        bool harvest();
        bool can_harvest();
        bool plant(int entity);
        void till();
        bool swap(int direction);
        void clear();
        bool use_item(int item, int count);
        int get_pos_x();
        int get_pos_y();
        int get_world_size();
        int get_entity_type_code();
        int get_ground_type_code();
        double get_water();
        long num_items(int item);
        double get_time();
        long get_tick_count();

        // ---- utilities ----
        double? measure();
        double? measure_at(int direction);
        int[]? get_companion();         // [entityCode, x, y] or null
        int[] get_cost(int entity);     // flat [itemId, count, ...] pairs
        double random();
        int num_drones();
        int max_drones();
        int add_drone();            // spawn a drone from this one; returns the new drone id
        int drone_generation();     // current farm drone generation (for DroneHandle)
        void remove_drone(int id);  // retire a spawned drone
        bool unlock(string name);
        int num_unlocked(string name);
        void set_execution_speed(double speed);
        void set_world_size(int size);
        void do_a_flip();
        void pet_the_piggy();
        void change_hat(string name);
        void quick_print(string text);
    }

    /// <summary>Headless bridge for the CLI and tests: records every call, returns canned values.</summary>
    public sealed class StubGameBridge : IGameBridge
    {
        public List<string> Calls { get; } = new();
        private int _droneCounter;

        /// <summary>Override for `get_companion` (null by default → None).</summary>
        public int[]? CompanionResult { get; set; }

        /// <summary>
        /// When set, action calls account their standard op cost into this
        /// accumulator (mirrors the real bridge's pacing budget), so tick-engine
        /// tests can verify computation + action op merging.
        /// </summary>
        public OpAccumulator? Ops { get; set; }

        private const double ActionCost = 200.0;
        private const double FlipCost = 400.0;

        public bool move(int direction) { Record($"move({direction})"); AddOps(ActionCost); return true; }
        public bool can_move(int direction) { Record($"can_move({direction})"); return true; }
        public bool harvest() { Record("harvest()"); AddOps(ActionCost); return true; }
        public bool can_harvest() { Record("can_harvest()"); return true; }
        public bool plant(int entity) { Record($"plant({entity})"); AddOps(ActionCost); return true; }
        public void till() { Record("till()"); AddOps(ActionCost); }
        public bool swap(int direction) { Record($"swap({direction})"); AddOps(ActionCost); return true; }
        public void clear() { Record("clear()"); AddOps(ActionCost); }
        public bool use_item(int item, int count) { Record($"use_item({item}, {count})"); AddOps(ActionCost); return true; }
        public int get_pos_x() { Record("get_pos_x()"); return 0; }
        public int get_pos_y() { Record("get_pos_y()"); return 0; }
        public int get_world_size() { Record("get_world_size()"); return 3; }
        public int get_entity_type_code() { Record("get_entity_type()"); return 1; }
        public int get_ground_type_code() { Record("get_ground_type()"); return 1; }
        public double get_water() { Record("get_water()"); return 0.0; }
        public long num_items(int item) { Record($"num_items({item})"); return 0; }
        public double get_time() { Record("get_time()"); return 0.0; }
        public long get_tick_count() { Record("get_tick_count()"); return Ops?.TotalOps ?? 0; }

        // ---- utilities (canned, deterministic) ----
        public double? measure() { Record("measure()"); return 1.5; }
        public double? measure_at(int direction) { Record($"measure_at({direction})"); return null; }
        public int[]? get_companion() { Record("get_companion()"); return CompanionResult; }
        public int[] get_cost(int entity) { Record($"get_cost({entity})"); return new[] { 3, 2, 8, 1 }; } // [(Carrot,2),(Water,1)]
        public double random() { Record("random()"); return 0.5; }
        public int num_drones() { Record("num_drones()"); return 1; }
        public int max_drones() { Record("max_drones()"); return 4; }
        public int add_drone() { Record("add_drone()"); AddOps(ActionCost); return ++_droneCounter; }
        public int drone_generation() { Record("drone_generation()"); return _droneCounter; }
        public void remove_drone(int id) { Record($"remove_drone({id})"); }
        public bool unlock(string name) { Record($"unlock({name})"); AddOps(ActionCost); return true; }
        public int num_unlocked(string name) { Record($"num_unlocked({name})"); return 2; }
        public void set_execution_speed(double speed) { Record($"set_execution_speed({speed})"); AddOps(ActionCost); }
        public void set_world_size(int size) { Record($"set_world_size({size})"); AddOps(ActionCost); }
        public void do_a_flip() { Record("do_a_flip()"); AddOps(FlipCost); }
        public void pet_the_piggy() { Record("pet_the_piggy()"); AddOps(FlipCost); }
        public void change_hat(string name) { Record($"change_hat({name})"); AddOps(ActionCost); }
        public void quick_print(string text) { Record($"quick_print({text})"); }

        private void Record(string call) => Calls.Add(call);

        private void AddOps(double ops)
        {
            if (Ops != null && ops > 0) Ops.Add((long)ops);
        }
    }
}