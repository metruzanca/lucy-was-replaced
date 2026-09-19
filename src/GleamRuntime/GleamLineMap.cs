using System;
using System.Collections.Generic;

namespace GleamRuntime
{
    /// <summary>
    /// Receives the Gleam source line being executed on the worker thread (one call per
    /// line change, per module). The plugin forwards these to the editor overlay.
    /// </summary>
    public interface IGleamLineSink
    {
        void OnLine(string module, int gleamLine);
    }
    /// <summary>
    /// Maps executed JavaScript step nodes back to Gleam source lines for one compiled
    /// module. Keys are the step node's start position in the compiled JS
    /// (1-based line, 0-based column); values are 1-based Gleam source lines.
    /// </summary>
    public sealed class GleamLineMap
    {
        private readonly Dictionary<(int Line, int Col), int> _byLocation;
        private readonly int _defaultLine;

        internal GleamLineMap(Dictionary<(int, int), int> byLocation, int defaultLine)
        {
            _byLocation = byLocation;
            _defaultLine = defaultLine;
        }

        /// <summary>How many JS step positions were mapped (informational / tests).</summary>
        public int Count => _byLocation.Count;

        /// <summary>Gleam line for a JS step at the given start position, or the module default.</summary>
        public int Get(int jsLine, int jsColumn)
            => _byLocation.TryGetValue((jsLine, jsColumn), out var line) ? line : _defaultLine;

        /// <summary>True when the position has an explicit mapping.</summary>
        public bool TryGet(int jsLine, int jsColumn, out int gleamLine)
            => _byLocation.TryGetValue((jsLine, jsColumn), out gleamLine);
    }
}