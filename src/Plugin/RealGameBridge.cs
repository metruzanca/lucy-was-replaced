using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using GleamRuntime;

namespace GleamFarmer
{
    /// <summary>
    /// Bridges Gleam's `game` module to the real game. Actions are dispatched to
    /// the Unity main thread (world/state mutations) and then pace the worker for
    /// `ops * OpDuration` real seconds — the same op-accounted timing the game's
    /// own interpreter uses. Sensors read state and return immediately.
    /// </summary>
    public sealed class RealGameBridge : IGameBridge, IGleamPrintHandler
    {
        private static readonly string[] EntityNames =
            { "", "Grass", "Bush", "Carrot", "Pumpkin", "Sunflower", "Tree", "Cactus", "Treasure", "Hedge" };

        private static readonly string[] ItemNames =
            { "", "Hay", "Wood", "Carrot", "Pumpkin", "Power", "Gold", "Bones", "Water", "Fertilizer" };

        private readonly MainThreadDispatcher _dispatcher;
        private readonly IGleamRunController _run;
        private readonly IGleamLogSink _log;
        private long _totalOps;

        public RealGameBridge(MainThreadDispatcher dispatcher, IGleamRunController run, IGleamLogSink log)
        {
            _dispatcher = dispatcher;
            _run = run;
            _log = log;
        }

        private static Simulation? Sim => MainSim.Inst?.sim;

        private T OnMain<T>(Func<Simulation, Drone, T> fn) => _dispatcher.Invoke(() =>
        {
            var sim = Sim;
            if (sim?.farm?.drones is not { Count: > 0 })
                throw new InvalidOperationException("The farm is not ready yet.");
            return fn(sim, sim.farm.drones[0]);
        });

        private static ProgramState NewProgramState() => new(0, new Random(), 0);

        private void WaitOps(double ops)
        {
            if (ops <= 0) return;
            Interlocked.Add(ref _totalOps, (long)ops);

            var sim = Sim;
            if (sim == null) return;
            var milliseconds = (int)(ops * sim.OpDuration.Seconds * 1000);
            if (milliseconds <= 0) return;

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
            {
                if (_run.IsStopped) throw new GleamStoppedException();
                Thread.Sleep(Math.Min(50, milliseconds - (int)stopwatch.ElapsedMilliseconds));
            }
        }

        // ---- actions (paced) ----

        public bool move(int direction)
        {
            var (ok, ops) = OnMain((sim, drone) =>
            {
                var moved = drone.Move((GridDirection)direction, NewProgramState(), out var ops);
                return (moved, ops);
            });
            WaitOps(ops);
            return ok;
        }

        public bool can_move(int direction) => OnMain((sim, drone) => drone.CanMove((GridDirection)direction));

        public bool harvest()
        {
            var ok = OnMain((sim, drone) => drone.Harvest());
            WaitOps(ok ? 200.0 : 1.0);
            return ok;
        }

        public bool can_harvest() => OnMain((sim, drone) => drone.CanHarvest());

        public bool plant(int entity)
        {
            var name = entity >= 0 && entity < EntityNames.Length ? EntityNames[entity] : string.Empty;
            var ok = OnMain((sim, drone) =>
            {
                if (string.IsNullOrEmpty(name)) return false;
                var farmObject = ResourceManager.GetFarmObject(name);
                return farmObject != null && drone.Plant(farmObject, NewProgramState());
            });
            WaitOps(ok ? 200.0 : 1.0);
            return ok;
        }

        public void till()
        {
            OnMain((sim, drone) =>
            {
                drone.ChangeGround("soil");
                return true;
            });
            WaitOps(200.0);
        }

        public bool use_item(int item)
        {
            var name = item >= 0 && item < ItemNames.Length ? ItemNames[item] : string.Empty;
            var ok = OnMain((sim, drone) =>
            {
                if (name != "Water") return false; // v1: only watering
                return drone.Water(1);
            });
            WaitOps(ok ? 200.0 : 1.0);
            return ok;
        }

        // ---- sensors (instant) ----

        public int get_pos_x() => OnMain((sim, drone) => drone.pos.x);
        public int get_pos_y() => OnMain((sim, drone) => drone.pos.y);
        public int get_world_size() => OnMain((sim, drone) => sim.farm.grid.WorldSize.y);

        public int get_entity_type_code() => OnMain((sim, drone) =>
        {
            var entity = drone.EntityUnderDrone();
            if (entity?.objectSO == null) return 0;
            return Array.IndexOf(EntityNames, entity.objectSO.objectName);
        });

        public int get_ground_type_code() => OnMain((sim, drone) =>
            drone.GroundUnderDrone()?.objectSO?.objectName == "Soil" ? 1 : 2);

        public double get_water() => OnMain((sim, drone) => drone.GetWater());

        public int num_items(int item)
        {
            var name = item >= 0 && item < ItemNames.Length ? ItemNames[item] : string.Empty;
            return OnMain((sim, drone) =>
            {
                if (string.IsNullOrEmpty(name)) return 0;
                var itemSo = ResourceManager.GetAllItems().FirstOrDefault(x => x.itemName == name);
                return itemSo == null ? 0 : (int)sim.farm.Items.GetNumber(itemSo.itemId);
            });
        }

        public double get_time() => OnMain((sim, drone) => sim.CurrentTime.Seconds);
        public int get_tick_count() => (int)Interlocked.Read(ref _totalOps);

        // ---- print (io.println) ----

        public void Print(string text)
        {
            _log.Log(text);
            OnMain((sim, drone) =>
            {
                drone.PrintToAir(text);
                return true;
            });
            WaitOps(Math.Floor(1.0 / (Sim?.OpDuration.Seconds ?? 0.0025)));
        }
    }
}