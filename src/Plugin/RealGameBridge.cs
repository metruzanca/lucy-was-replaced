using System;
using System.Collections.Generic;
using System.Linq;
using GleamRuntime;

namespace GleamFarmer
{
    /// <summary>
    /// Bridges Gleam's `game` module to the real game. Actions are dispatched to
    /// the Unity main thread (world/state mutations) and then pace the worker for
    /// `ops * OpDuration` real seconds — the same op-accounted timing the game's
    /// own interpreter uses. All ops (actions and pure computation) feed the
    /// shared <see cref="TickEngine"/> counter and drain power like the game.
    /// Sensors read state and return immediately.
    /// </summary>
    public sealed class RealGameBridge : IGameBridge, IGleamPrintHandler
    {
        // Game objectName/itemName values are lowercase (e.g. "carrot", "soil", "bone").
        private static readonly string[] EntityNames =
            { "", "grass", "bush", "carrot", "pumpkin", "sunflower", "tree", "cactus", "treasure", "hedge" };

        private static readonly string[] ItemNames =
            { "", "hay", "wood", "carrot", "pumpkin", "power", "gold", "bone", "water", "fertilizer" };

        private readonly MainThreadDispatcher _dispatcher;
        private readonly IGleamLogSink _log;
        private readonly TickEngine _ticks;
        private readonly Random _random = new();
        private double _opDuration = 0.0025;

        public RealGameBridge(
            MainThreadDispatcher dispatcher,
            IGleamRunController run,
            IGleamLogSink log,
            TickEngine ticks)
        {
            _dispatcher = dispatcher;
            _log = log;
            _ticks = ticks;
            _ticks.Pacer.OnBatch = FlushPower;
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

        // ResourceManager loads lazily at game start; lookups can throw for a
        // moment. Treat "not ready" as a plain miss instead of crashing the run.
        private static FarmObjectSO? TryGetFarmObject(string name)
        {
            try { return ResourceManager.GetFarmObject(name); }
            catch { return null; }
        }

        private static ItemSO? TryGetItem(string name)
        {
            try { return ResourceManager.GetAllItems().FirstOrDefault(x => x.itemName == name); }
            catch { return null; }
        }

        private static UnlockSO? TryGetUnlock(string name)
        {
            try { return ResourceManager.GetUnlock(name); }
            catch { return null; }
        }

        private static HatSO? TryGetHat(string name)
        {
            try { return ResourceManager.GetHat(name); }
            catch { return null; }
        }

        private void WaitOps(double ops)
        {
            if (ops <= 0) return;
            var sim = Sim;
            if (sim != null) _opDuration = sim.OpDuration.Seconds;
            _ticks.Pacer.OpDurationSeconds = _opDuration;
            _ticks.Pacer.Account((long)ops);
        }

        /// <summary>
        /// Drain the game's power resource for consumed ops, mirroring
        /// Execution.Execute's `UsedPower += ops / 200 / 30`.
        /// </summary>
        private void FlushPower(long ops)
        {
            _dispatcher.Invoke(() =>
            {
                var sim = Sim;
                if (sim?.farm != null) sim.farm.UsedPower += ops / 200.0 / 30.0;
                return true;
            });
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
                var farmObject = TryGetFarmObject(name);
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

        public bool swap(int direction)
        {
            var ok = OnMain((sim, drone) => drone.Swap((GridDirection)direction, NewProgramState()));
            WaitOps(ok ? 200.0 : 1.0);
            return ok;
        }

        public void clear()
        {
            OnMain((sim, drone) =>
            {
                sim.farm.RemoveSpawnedDrones();
                sim.farm.drones[0].ResetPos();
                sim.farm.grid.ClearGrid();
                return true;
            });
            WaitOps(200.0);
        }

        public bool use_item(int item, int count)
        {
            var name = item >= 0 && item < ItemNames.Length ? ItemNames[item] : string.Empty;
            var ok = OnMain((sim, drone) =>
            {
                if (count < 1 || string.IsNullOrEmpty(name)) return false;
                var itemSo = TryGetItem(name);
                if (itemSo == null || !sim.farm.Items.Contains(itemSo.itemId, count)) return false;
                var used = name switch
                {
                    "water" => drone.Water(count),
                    "fertilizer" => drone.Fertilize(count),
                    _ => false,
                };
                if (used) sim.farm.Items.RemoveItem(itemSo.itemId, count);
                return used;
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
            drone.GroundUnderDrone()?.objectSO?.objectName == "soil" ? 1 : 2);

        public double get_water() => OnMain((sim, drone) => drone.GetWater());

        public long num_items(int item)
        {
            var name = item >= 0 && item < ItemNames.Length ? ItemNames[item] : string.Empty;
            return OnMain((sim, drone) =>
            {
                if (string.IsNullOrEmpty(name)) return 0L;
                var itemSo = TryGetItem(name);
                return itemSo == null ? 0L : (long)sim.farm.Items.GetNumber(itemSo.itemId);
            });
        }

        public double get_time() => _ticks.Ops.TotalOps * _opDuration;
        public long get_tick_count() => _ticks.Ops.TotalOps;

        // ---- utility sensors (instant) ----

        public double? measure() => OnMain((sim, drone) =>
            MeasureOf(drone.EntityUnderDrone()?.Measure()));

        public double? measure_at(int direction) => OnMain((sim, drone) =>
        {
            var dir = (GridDirection)direction;
            var key = sim.farm.grid.Wrap(drone.pos + dir.GetDirectionVector());
            if (!sim.farm.grid.entities.TryGetValue(key, out var entity)) return (double?)null;
            return MeasureOf(entity.Measure());
        });

        private static double? MeasureOf(object? value) =>
            value is PyNumber number ? (double)number : (double?)null;

        public int[]? get_companion() => OnMain<int[]?>((sim, drone) =>
        {
            if (!sim.farm.grid.entities.TryGetValue(drone.pos, out var entity) || entity is not Growable growable)
                return null;
            var companion = growable.GetCompanion();
            if (companion is not PyTuple tuple
                || tuple.Count != 2
                || tuple[0] is not FarmObjectSO so
                || tuple[1] is not PyTuple pos
                || pos.Count != 2
                || pos[0] is not PyNumber px
                || pos[1] is not PyNumber py)
                return null;
            return new[] { Array.IndexOf(EntityNames, so.objectName), (int)(double)px, (int)(double)py };
        });

        public int[] get_cost(int entity)
        {
            var name = entity >= 0 && entity < EntityNames.Length ? EntityNames[entity] : string.Empty;
            return OnMain((sim, drone) =>
            {
                if (string.IsNullOrEmpty(name)) return Array.Empty<int>();
                var farmObject = TryGetFarmObject(name);
                if (farmObject == null) return Array.Empty<int>();

                var cost = farmObject.cost;
                var factor = 1;
                if (!string.IsNullOrEmpty(farmObject.yieldUpgradeName))
                    factor = 1 << Math.Max(0, sim.farm.NumUnlocked(farmObject.yieldUpgradeName) - 1);

                var flat = new List<int>();
                for (var i = 0; i < cost.items.Length; i++)
                {
                    var count = cost.items[i] * factor;
                    if (count > 0) { flat.Add(i); flat.Add((int)count); }
                }
                return flat.ToArray();
            });
        }

        public double random() => _random.NextDouble();
        public int num_drones() => OnMain((sim, drone) => sim.farm.drones.Count);
        public int max_drones() => OnMain((sim, drone) => Helper.NumDrones(sim.farm.NumUnlocked("megafarm")));
        public int num_unlocked(string name) => OnMain((sim, drone) =>
            string.IsNullOrEmpty(name) ? 0 : sim.farm.NumUnlocked(name));

        public bool unlock(string name)
        {
            var ok = OnMain((sim, drone) =>
            {
                if (string.IsNullOrEmpty(name)) return false;
                var unlock = TryGetUnlock(name);
                return unlock != null && sim.farm.UnlockOrUpgrade(unlock, requireParent: false);
            });
            WaitOps(ok ? 200.0 : 1.0);
            return ok;
        }

        public void set_execution_speed(double speed)
        {
            OnMain((sim, drone) =>
            {
                if (double.IsNaN(speed) || speed > sim.farm.MaxSpeedFactor() || speed < 0.1)
                    sim.ChangeExecutionSpeed(sim.farm.MaxSpeedFactor());
                else
                    sim.ChangeExecutionSpeed(speed);
                return true;
            });
            WaitOps(200.0);
        }

        public void set_world_size(int size)
        {
            OnMain((sim, drone) =>
            {
                if (size != sim.farm.grid.WorldSize.y)
                {
                    foreach (var d in sim.farm.drones) d?.ResetPos();
                    sim.farm.grid.SizeLimit = size;
                }
                return true;
            });
            WaitOps(200.0);
        }

        public void do_a_flip()
        {
            OnMain((sim, drone) =>
            {
                drone.DoAFlip();
                return true;
            });
            WaitOps(FlipOps());
        }

        public void pet_the_piggy()
        {
            OnMain((sim, drone) =>
            {
                drone.PetThePiggy();
                return true;
            });
            WaitOps(FlipOps());
        }

        private double FlipOps() => Math.Floor(1.0 / (Sim?.OpDuration.Seconds ?? 0.0025));

        public void change_hat(string name)
        {
            OnMain((sim, drone) =>
            {
                if (string.IsNullOrEmpty(name)) return false;
                var hat = TryGetHat(name);
                if (hat == null) return false;
                drone.ChangeHat(hat, NewProgramState());
                return true;
            });
            WaitOps(200.0);
        }

        // ---- print (io.println) ----

        public void Print(string text)
        {
            _log.Log(text);
            OnMain((sim, drone) =>
            {
                drone.PrintToAir(text);
                return true;
            });
            WaitOps(FlipOps());
        }

        // ---- quick_print (free print, no pacing) ----

        public void quick_print(string text) => _log.Log(text);
    }
}