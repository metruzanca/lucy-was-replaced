using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GleamRuntime;

namespace GleamFarmer
{
    /// <summary>
    /// Routes executed-Gleam-line events from the worker thread to the game's
    /// <c>BlinkManager</c> overlay on the Unity main thread. Resolves the module name to its
    /// code window and converts the 1-based Gleam line to character offsets into the window
    /// text (snapshotted when the run starts, since editing a window stops the run).
    /// Line changes are coalesced; the main thread drains the queue once per frame.
    /// </summary>
    public sealed class GleamHighlightBridge : IGleamLineSink
    {
        private readonly IReadOnlyDictionary<string, (CodeWindow Window, int[] LineOffsets)> _windows;
        private readonly ConcurrentQueue<(CodeWindow Window, int Start, int End)> _pending = new();
        private readonly Dictionary<string, int> _lastLine = new();
        private (CodeWindow Window, int Start, int End)? _lastBlink;

        /// <param name="windows">Module name → (code window, 1-based line → char offset) map.</param>
        public GleamHighlightBridge(IReadOnlyDictionary<string, (CodeWindow, int[])> windows)
        {
            _windows = windows;
        }

        public bool HasWindows => _windows.Count > 0;

        public void OnLine(string module, int gleamLine)
        {
            if (!_windows.TryGetValue(module, out var entry)) return;
            if (_lastLine.TryGetValue(module, out var last) && last == gleamLine) return;
            _lastLine[module] = gleamLine;

            var (window, offsets) = entry;
            if (gleamLine < 1 || gleamLine > offsets.Length) return;
            var start = offsets[gleamLine - 1];
            var end = gleamLine < offsets.Length ? offsets[gleamLine] : int.MaxValue;
            _lastBlink = (window, start, end);
            _pending.Enqueue((window, start, end));
        }

        /// <summary>
        /// Re-queue the most recent line so it stays lit while paused in step-through mode
        /// (the game's overlay re-fades it before it would otherwise vanish). Called on the
        /// main thread, throttled.
        /// </summary>
        public void ReBlink()
        {
            if (_lastBlink is { } blink) _pending.Enqueue(blink);
        }

        /// <summary>Clear the coalescing state (called when a run finishes).</summary>
        public void Reset()
        {
            _lastLine.Clear();
            while (_pending.TryDequeue(out _)) { }
        }

        /// <summary>Drain pending highlights into the game's overlay. Must run on the main thread.</summary>
        public void Pump()
        {
            if (MainSim.Inst == null) return;
            var drained = 0;
            while (_pending.TryDequeue(out var item) && drained < 500)
            {
                var text = item.Window.CodeInput.text;
                var start = item.Start;
                var end = item.End == int.MaxValue ? text.Length : item.End;
                // Guard the TMP char lookups BlinkManager does: wordStart/wordEnd must be
                // valid indices and wordEnd > wordStart (an empty line blinks nothing).
                if (start >= text.Length) start = Math.Max(0, text.Length - 1);
                if (end > text.Length) end = text.Length;
                if (end <= start) continue;
                MainSim.Inst.BlinkEffect(new GleamBlinkNode(item.Window, start, end));
                drained++;
            }
        }
    }
}