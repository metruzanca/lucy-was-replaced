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
        private readonly IGleamLineSink? _lineSink;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly StepGate _gate = new();
        private volatile bool _stopped;

        public PacedGleamRun(
            CompiledGleam compiled,
            MainThreadDispatcher dispatcher,
            IGleamLogSink log,
            IGleamLineSink? lineSink = null)
        {
            _compiled = compiled;
            _dispatcher = dispatcher;
            _log = log;
            _lineSink = lineSink;
        }

        public bool IsStopped => _stopped;

        /// <summary>Step-through gate for this run (available from construction).</summary>
        public StepGate StepGate => _gate;

        /// <summary>Raised on the worker thread when the program finishes or is stopped.</summary>
        public event Action? Completed;

        /// <summary>Raised on the worker thread when the program fails at runtime.</summary>
        public event Action<Exception>? Failed;

        public void Start()
        {
            _stopped = false;
            new Thread(Run) { IsBackground = true, Name = "GleamFarmer" }.Start();
        }

        /// <summary>Abort the run at the next action boundary (or interrupt JS).</summary>
        public void Stop()
        {
            _stopped = true;
            // A worker paused on the step gate must be released now, or it never unwinds
            // (it is blocked in the gate, not in a Jint statement the token can interrupt).
            _gate.Abort();
            try { _cancellation.Cancel(); } catch (ObjectDisposedException) { }
        }

        /// <summary>Release any engine paused in step-through mode (stop/finish).</summary>
        private void ReleaseStepGate(TickEngine ticks) => ticks.StepGate.Abort();

        private void Run()
        {
            Exception? failure = null;
            DroneController? drones = null;
            TickEngine? ticks = null;
            try
            {
                // No wall-clock timeout: pacing waits accumulate real time. The
                // cancellation token handles Stop (including tight JS loops); the
                // recursion limit catches runaway recursion.
                ticks = new TickEngine(this, _gate);
                var mainBridge = new RealGameBridge(_dispatcher, this, _log, ticks, droneId: 0);
                drones = new DroneController(
                    _compiled.Sources, _log, TimeSpan.FromHours(12), ticks, this,
                    _cancellation.Token, _lineSink, _compiled.LineMaps,
                    id => new RealGameBridge(_dispatcher, this, _log, ticks, droneId: id));
                drones.DroneFailed += _ => _cancellation.Cancel();

                using var js = new JsRuntime(
                    _compiled.Sources, _log, TimeSpan.FromHours(12), mainBridge, mainBridge,
                    _cancellation.Token, ticks, new DroneBridge(drones, 0, mainBridge),
                    _compiled.LineMaps, _lineSink);
                js.RunMain();
            }
            catch (GleamStoppedException)
            {
                // expected: player pressed stop (or stepped off the end)
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
                // A worker paused on the step gate must be released, else it stays blocked
                // forever after stop/finish.
                if (ticks != null) ReleaseStepGate(ticks);
                drones?.Dispose();
                _cancellation.Dispose();
                failure ??= drones?.Failure;
                if (failure != null) Failed?.Invoke(failure);
                else Completed?.Invoke();
            }
        }

        /// <summary>
        /// Non-blocking: signals the worker to stop and returns immediately. The worker
        /// disposes the cancellation source itself in its <see cref="Run"/> finally block.
        /// Joining on the caller (Unity main thread) would stall it up to 2s while the
        /// worker finishes, and would deadlock if the worker is mid-dispatch waiting for
        /// the main thread to pump.
        /// </summary>
        public void Dispose() => Stop();
    }
}