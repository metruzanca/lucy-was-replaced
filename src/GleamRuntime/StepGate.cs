using System;
using System.Threading;

namespace GleamRuntime
{
    /// <summary>
    /// Step-through gate for the Gleam runtime, mirroring the game's step-by-step mode:
    /// when active, the executing engines pause at each new source line and only resume
    /// when <see cref="Next"/> is called (or step mode is exited/aborted).
    ///
    /// Shared across all engines (drones step together, like the game stepping every
    /// state). The gate is a closed semaphore while stepping: a step handler closes it on
    /// a new line, then blocks until it is opened; <see cref="Next"/> opens it once, and
    /// the next new line closes it again.
    /// </summary>
    public sealed class StepGate
    {
        private readonly object _lock = new();
        private bool _active;
        private bool _open;
        private bool _aborted;

        /// <summary>True while step-through mode is on.</summary>
        public bool IsActive
        {
            get { lock (_lock) return _active; }
        }

        /// <summary>Enable step-through mode: the next new line blocks.</summary>
        public void Enter()
        {
            lock (_lock)
            {
                _active = true;
                _open = false;
                _aborted = false;
            }
        }

        /// <summary>Disable step-through mode and release any blocked engines.</summary>
        public void Exit()
        {
            lock (_lock)
            {
                _active = false;
                _open = true;
                _aborted = false;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>Advance one line: release the blocked engines, which re-pause at the next new line.</summary>
        public void Next()
        {
            lock (_lock)
            {
                _open = true;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>Permanently release all blocked engines (used when a run stops mid-step).</summary>
        public void Abort()
        {
            lock (_lock)
            {
                _aborted = true;
                _open = true;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Close the gate so the next new line blocks (called by the step handler before
        /// waiting). Idempotent.
        /// </summary>
        public void Close()
        {
            lock (_lock)
            {
                if (_active && !_aborted) _open = false;
            }
        }

        /// <summary>
        /// Blocks while step mode is on and the gate is closed. Returns true when the engine
        /// may continue (Next/Exit), false when aborted (the run is stopping).
        /// </summary>
        public bool WaitForNext()
        {
            lock (_lock)
            {
                if (!_active || _aborted) return !_aborted;
                while (_active && !_open && !_aborted)
                    Monitor.Wait(_lock);
                return !_aborted;
            }
        }
    }
}