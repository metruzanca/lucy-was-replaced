using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GleamRuntime
{
    /// <summary>
    /// Drone-side FFI host, registered in each JsRuntime as `__gleam_drones`.
    /// A per-engine wrapper knows its drone id; all state lives in the shared
    /// <see cref="DroneController"/>.
    /// </summary>
    public interface IGameDroneHost
    {
        int[] spawn_drone(int count, string workerName);
        bool has_finished(int id, int generation);
        string? wait_for(int id, int generation);
        void send(string payloadJson, int toDroneId);
        string? receive(int fromDroneId);
        int get_drone_id();
    }

    /// <summary>Per-engine drone host bound to one drone id.</summary>
    public sealed class DroneBridge : IGameDroneHost
    {
        private readonly DroneController _controller;
        private readonly IGameBridge _bridge;
        private readonly int _droneId;

        public DroneBridge(DroneController controller, int droneId, IGameBridge bridge)
        {
            _controller = controller;
            _droneId = droneId;
            _bridge = bridge;
        }

        public int[] spawn_drone(int count, string workerName)
        {
            if (count <= 0)
                throw new InvalidOperationException("spawn_drone requires a positive count.");
            if (string.IsNullOrWhiteSpace(workerName))
                throw new InvalidOperationException(
                    "spawn_drone needs a named worker (a `pub fn`); anonymous functions cannot be spawned.");

            var flat = new int[count * 2];
            for (var i = 0; i < count; i++)
            {
                var id = _bridge.add_drone();              // world op (paced ~200 ops)
                var generation = _bridge.drone_generation();
                flat[i * 2] = id;
                flat[i * 2 + 1] = generation;
                _controller.StartDrone(id, generation, workerName);
            }
            return flat;
        }

        public bool has_finished(int id, int generation) => _controller.HasFinished(id, generation);

        public string? wait_for(int id, int generation) =>
            id == _droneId ? null : _controller.WaitFor(id, generation); // waiting on yourself would deadlock

        public void send(string payloadJson, int toDroneId) => _controller.Send(_droneId, payloadJson, toDroneId);

        public string? receive(int fromDroneId) => _controller.Receive(_droneId, fromDroneId);

        public int get_drone_id() => _droneId;
    }

    /// <summary>
    /// Runs spawned Gleam drones: one worker thread + one Jint engine per drone,
    /// sharing the run's module map, tick engine, cancellation and world bridge.
    /// The drone's worker is a named `pub fn` in the editor module, resolved by
    /// name in each fresh engine (Gleam preserves function names in its JS output).
    /// </summary>
    public sealed class DroneController : IDisposable
    {
        private readonly IReadOnlyDictionary<string, string> _moduleSources;
        private readonly IGleamLogSink? _sink;
        private readonly TimeSpan _timeout;
        private readonly TickEngine _ticks;
        private readonly IGleamRunController? _run;
        private readonly Func<int, IGameBridge> _bridgeFactory;
        private readonly IGleamLineSink? _lineSink;
        private readonly IReadOnlyDictionary<string, GleamLineMap>? _lineMaps;
        private readonly CancellationTokenSource _cancellation;
        private readonly ConcurrentDictionary<int, DroneSlot> _drones = new();
        private readonly ConcurrentDictionary<int, Mailbox> _mailboxes = new();
        private readonly object _failureLock = new();
        private volatile Exception? _failure;
        private int _stopped;

        public DroneController(
            IReadOnlyDictionary<string, string> moduleSources,
            IGleamLogSink? sink,
            TimeSpan timeout,
            TickEngine ticks,
            IGleamRunController? run,
            CancellationToken cancellation,
            IGleamLineSink? lineSink,
            IReadOnlyDictionary<string, GleamLineMap>? lineMaps,
            Func<int, IGameBridge> bridgeFactory)
        {
            _moduleSources = moduleSources;
            _sink = sink;
            _timeout = timeout;
            _ticks = ticks;
            _run = run;
            _lineSink = lineSink;
            _lineMaps = lineMaps;
            _bridgeFactory = bridgeFactory;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        }

        /// <summary>First drone failure, if any (used to surface child errors).</summary>
        public Exception? Failure => _failure;

        /// <summary>Raised on the failing drone's thread when a spawned drone errors.</summary>
        public event Action<Exception>? DroneFailed;

        public void StartDrone(int id, int generation, string workerName)
        {
            if (Volatile.Read(ref _stopped) != 0) return;
            var slot = new DroneSlot { Id = id, Generation = generation, Bridge = _bridgeFactory(id) };
            _drones[id] = slot;
            var thread = new Thread(() => RunDrone(slot, workerName))
            {
                IsBackground = true,
                Name = $"GleamFarmer-drone-{id}",
            };
            slot.Thread = thread;
            thread.Start();
        }

        public bool HasFinished(int id, int generation) =>
            _drones.TryGetValue(id, out var slot)
            && slot.Generation == generation
            && slot.Completed.IsSet;

        /// <summary>Block the calling thread until the target drone finishes; returns its JSON result.</summary>
        public string? WaitFor(int id, int generation)
        {
            if (!_drones.TryGetValue(id, out var slot) || slot.Generation != generation) return null;
            while (true)
            {
                if (_run != null && _run.IsStopped) throw new GleamStoppedException();
                if (_cancellation.IsCancellationRequested) throw new OperationCanceledException(_cancellation.Token);
                if (slot.Completed.Wait(50)) return slot.ResultJson;
                if (slot.Error != null) throw slot.Error;
            }
        }

        public void Send(int senderId, string payload, int toDroneId)
        {
            _mailboxes.GetOrAdd(toDroneId, _ => new Mailbox()).Enqueue(senderId, payload);
        }

        /// <summary>Pop a message for `droneId`, optionally from a specific sender (fromId &lt; 0 = any).</summary>
        public string? Receive(int droneId, int fromDroneId)
        {
            if (!_mailboxes.TryGetValue(droneId, out var mailbox)) return null;
            return mailbox.Dequeue(fromDroneId);
        }

        /// <summary>Stop all drones: cancel engines, release waiters, join threads.</summary>
        public void Stop()
        {
            if (Interlocked.Exchange(ref _stopped, 1) != 0) return;
            try { _cancellation.Cancel(); } catch (ObjectDisposedException) { }
            foreach (var slot in _drones.Values)
            {
                slot.Completed.Set();
                try { slot.Thread?.Join(500); } catch { }
            }
        }

        public void Dispose()
        {
            Stop();
            _cancellation.Dispose();
        }

        private void RunDrone(DroneSlot slot, string workerName)
        {
            try
            {
                var sources = new Dictionary<string, string>();
                foreach (var kv in _moduleSources) sources[kv.Key] = kv.Value;
                sources[$"__drone_{slot.Id}"] = SyntheticEntry(workerName);
                var drones = new DroneBridge(this, slot.Id, slot.Bridge);
                using var js = new JsRuntime(
                    sources, _sink, _timeout, slot.Bridge, slot.Bridge as IGleamPrintHandler,
                    _cancellation.Token, _ticks, drones, lineMaps: _lineMaps, lineSink: _lineSink);
                js.RunModule($"__drone_{slot.Id}");
                slot.ResultJson = js.ReadGlobalString("__gleam_drone_result");
                Complete(slot);
            }
            catch (GleamStoppedException)
            {
                Complete(slot);
            }
            catch (Jint.Runtime.ExecutionCanceledException)
            {
                Complete(slot);
            }
            catch (OperationCanceledException)
            {
                Complete(slot);
            }
            catch (Exception ex)
            {
                slot.Error = ex;
                Complete(slot);
                Fail(ex);
            }
        }

        private void Complete(DroneSlot slot)
        {
            if (!slot.Completed.IsSet) slot.Completed.Set();
            try { slot.Bridge.remove_drone(slot.Id); } catch { /* best effort */ }
        }

        private void Fail(Exception ex)
        {
            lock (_failureLock)
            {
                if (_failure == null) _failure = ex;
            }
            DroneFailed?.Invoke(ex);
        }

        private static string SyntheticEntry(string workerName) =>
            $"import {{ {workerName} }} from \"./main.mjs\";\n" +
            $"globalThis.__gleam_drone_result = JSON.stringify(({workerName})());\n";

        private sealed class DroneSlot
        {
            public int Id;
            public int Generation;
            public IGameBridge Bridge = null!;
            public Thread Thread = null!;
            public ManualResetEventSlim Completed = new(false);
            public volatile string? ResultJson;
            public volatile Exception? Error;
        }

        private sealed class Mailbox
        {
            private readonly object _gate = new();
            private readonly Queue<(int Sender, string Payload)> _items = new();

            public void Enqueue(int sender, string payload)
            {
                lock (_gate) _items.Enqueue((sender, payload));
            }

            public string? Dequeue(int fromDroneId)
            {
                lock (_gate)
                {
                    if (fromDroneId < 0)
                        return _items.Count > 0 ? _items.Dequeue().Payload : null;

                    var found = default((int Sender, string Payload)?);
                    var buffer = new List<(int Sender, string Payload)>();
                    while (_items.Count > 0)
                    {
                        var item = _items.Dequeue();
                        if (found == null && item.Sender == fromDroneId) { found = item; continue; }
                        buffer.Add(item);
                    }
                    foreach (var item in buffer) _items.Enqueue(item);
                    return found?.Payload;
                }
            }
        }
    }
}