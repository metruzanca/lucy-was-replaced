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
        int get_pos_x();
        int get_pos_y();
        int get_world_size();
        int get_entity_type_code();
        int get_ground_type_code();
        double get_water();
        long num_items(int item);
        bool use_item(int item);
        double get_time();
        long get_tick_count();
    }

    /// <summary>Headless bridge for the CLI and tests: records every call, returns canned values.</summary>
    public sealed class StubGameBridge : IGameBridge
    {
        public List<string> Calls { get; } = new();

        public bool move(int direction) { Record($"move({direction})"); return true; }
        public bool can_move(int direction) { Record($"can_move({direction})"); return true; }
        public bool harvest() { Record("harvest()"); return true; }
        public bool can_harvest() { Record("can_harvest()"); return true; }
        public bool plant(int entity) { Record($"plant({entity})"); return true; }
        public void till() { Record("till()"); }
        public int get_pos_x() { Record("get_pos_x()"); return 0; }
        public int get_pos_y() { Record("get_pos_y()"); return 0; }
        public int get_world_size() { Record("get_world_size()"); return 3; }
        public int get_entity_type_code() { Record("get_entity_type()"); return 1; }
        public int get_ground_type_code() { Record("get_ground_type()"); return 1; }
        public double get_water() { Record("get_water()"); return 0.0; }
        public long num_items(int item) { Record($"num_items({item})"); return 0; }
        public bool use_item(int item) { Record($"use_item({item})"); return true; }
        public double get_time() { Record("get_time()"); return 0.0; }
        public long get_tick_count() { Record("get_tick_count()"); return 0; }

        private void Record(string call) => Calls.Add(call);
    }
}