using System;
using System.Threading;
using GleamRuntime;

namespace GleamFarmer
{
    /// <summary>
    /// Runs a compiled Gleam package on a dedicated worker thread so game actions
    /// can block (pace) while the world keeps running on the main thread.
    /// </summary>
    public sealed class PacedGleamRun : IGleamRunController, IDisposable
    {
        private readonly CompiledGleam _compiled;
        private readonly MainThreadDispatcher _dispatcher;
        private readonly IGleamLogSink _log;
        private readonly CancellationTokenSource _cancellation = new();
        private Thread? _thread;
        private volatile bool _stopped;

        public PacedGleamRun(CompiledGleam compiled, MainThreadDispatcher dispatcher, IGleamLogSink log)
        {
            _compiled = compiled;
            _dispatcher = dispatcher;
            _log = log;
        }

        public bool IsStopped => _stopped;

        /// <summary>Raised on the worker thread when the program finishes or is stopped.</summary>
        public event Action? Completed;

        /// <summary>Raised on the worker thread when the program fails at runtime.</summary>
        public event Action<Exception>? Failed;

        public void Start()
        {
            _stopped = false;
            _thread = new Thread(Run) { IsBackground = true, Name = "GleamFarmer" };
            _thread.Start();
        }

        /// <summary>Abort the run at the next action boundary (or interrupt JS).</summary>
        public void Stop()
        {
            _stopped = true;
            try { _cancellation.Cancel(); } catch (ObjectDisposedException) { }
        }

        private void Run()
        {
            Exception? failure = null;
            DroneController? drones = null;
            try
            {
                // No wall-clock timeout: pacing waits accumulate real time. The
                // cancellation token handles Stop (including tight JS loops); the
                // recursion limit catches runaway recursion.
                var ticks = new TickEngine(this);
                var mainBridge = new RealGameBridge(_dispatcher, this, _log, ticks, droneId: 0);
                drones = new DroneController(
                    _compiled.Sources, _log, TimeSpan.FromHours(12), ticks, this,
                    _cancellation.Token,
                    id => new RealGameBridge(_dispatcher, this, _log, ticks, droneId: id));
                drones.DroneFailed += _ => _cancellation.Cancel();

                using var js = new JsRuntime(
                    _compiled.Sources, _log, TimeSpan.FromHours(12), mainBridge, mainBridge,
                    _cancellation.Token, ticks, new DroneBridge(drones, 0, mainBridge));
                js.RunMain();
            }
            catch (GleamStoppedException)
            {
                // expected: player pressed stop
            }
            catch (Jint.Runtime.ExecutionCanceledException)
            {
                // expected: player pressed stop during a JS compute burst
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                _stopped = true;
                drones?.Dispose();
                failure ??= drones?.Failure;
                if (failure != null) Failed?.Invoke(failure);
                else Completed?.Invoke();
            }
        }

        public void Dispose()
        {
            Stop();
            _thread?.Join(2000);
            _cancellation.Dispose();
        }
    }
}