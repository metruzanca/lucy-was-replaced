using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Acornima;
using Acornima.Ast;

namespace GleamRuntime
{
    /// <summary>
    /// The game's op-accounted tick model for a single Gleam run, mirroring
    /// `Execution`/`ProgramState` in the real interpreter: every executed
    /// statement (and the game-action ops the bridge accounts) feeds one op
    /// counter, and execution is paced to `ops * OpDuration` real seconds
    /// (400 ops/s at 1x).
    ///
    /// Counting is done with Jint's debugger <c>Step</c> event: it fires once per
    /// executed statement and exposes the Acornima AST node, which
    /// <see cref="OpWeights"/> converts to the game's op cost for that statement.
    ///
    /// One engine per drone shares a single <see cref="TickEngine"/>, so the whole
    /// farm keeps one global tick rate (mirroring the game's `GlobalOpCount`).
    /// <see cref="OpWeights"/> caches are per-engine (AST nodes are not shared
    /// across engines).
    /// </summary>
    public sealed class TickEngine
    {
        public TickEngine(IGleamRunController? run = null)
        {
            Ops = new OpAccumulator();
            Pacer = new TickPacer(Ops, run);
        }

        /// <summary>Total ops consumed so far (computation + actions).</summary>
        public OpAccumulator Ops { get; }

        /// <summary>Real-time pacing to `ops * OpDuration`.</summary>
        public TickPacer Pacer { get; }
    }

    /// <summary>Thread-safe total-op counter backing `get_tick_count`.</summary>
    public sealed class OpAccumulator
    {
        private long _totalOps;

        public long TotalOps => Interlocked.Read(ref _totalOps);

        public void Add(long ops)
        {
            if (ops > 0) Interlocked.Add(ref _totalOps, ops);
        }

        public void Reset() => Interlocked.Exchange(ref _totalOps, 0);
    }

    /// <summary>
    /// Maps a Jint debugger <c>Step</c> AST node to the game's op cost. Computed
    /// once per node instance and cached (the interpreter reuses the same AST
    /// nodes every iteration, so each step is an O(1) lookup).
    ///
    /// Costs follow the documented tick model: each binary operation = 1 tick,
    /// an if branch = 1 tick, a loop start = 1 tick, indexing = 1 tick, while
    /// function calls and variable reads/writes are free.
    /// </summary>
    public sealed class OpWeights
    {
        /// <summary>Stop caching past this many unique nodes (memory guard).</summary>
        private const int CacheLimit = 100_000;

        private readonly Dictionary<Node, int> _cache = new(RefComparer.Instance);
        private bool _caching = true;

        public int Get(Node? node)
        {
            if (node == null) return 0;
            if (_caching && _cache.TryGetValue(node, out var cached)) return cached;
            var weight = Compute(node);
            if (_caching)
            {
                if (_cache.Count >= CacheLimit) _caching = false;
                else _cache[node] = weight;
            }
            return weight;
        }

        private int Compute(Node node) => node switch
        {
            // ---- statements ----
            IfStatement s => 1 + Get(s.Test),          // branch + condition (condition not stepped separately)
            WhileStatement => 1,                        // loop start; test is stepped separately
            DoWhileStatement => 1,
            ForStatement => 1,                          // init/test/update stepped separately
            ForInStatement => 1,
            ForOfStatement => 1,
            SwitchStatement => 1,
            FunctionDeclaration => 1,                   // function definition (body stepped per call)
            FunctionExpression => 0,                    // closures: body stepped per call
            ArrowFunctionExpression f => f.Body is BlockStatement ? 0 : Get(f.Body),
            VariableDeclaration => SumChildren(node),   // declarator inits
            VariableDeclarator v => Get(v.Init),
            ExpressionStatement s => Get(s.Expression),
            ReturnStatement s => Get(s.Argument),
            ThrowStatement s => Get(s.Argument),
            TryStatement => 0,
            LabeledStatement => 0,
            WithStatement => 0,
            BlockStatement => 0,                        // containers are free; contents stepped separately
            BreakStatement => 0,
            ContinueStatement => 0,
            EmptyStatement => 0,
            DebuggerStatement => 0,

            // ---- expressions ----
            UpdateExpression => 1,                      // i++ / i--
            UnaryExpression u => u.Operator is Operator.UnaryNegation or Operator.LogicalNot
                ? Get(u.Argument)                        // unary - and not are free
                : 1 + Get(u.Argument),
            AssignmentExpression a => a.Operator == Operator.Assignment
                ? SumChildren(a)                         // plain write is free
                : 1 + SumChildren(a),                    // += and friends cost the arithmetic
            LogicalExpression b => 1 + SumChildren(b),
            BinaryExpression b => 1 + SumChildren(b),    // loop-test conditions step as binaries
            ConditionalExpression c => 1 + SumChildren(c),
            MemberExpression m => (m.Computed ? 1 : 0) + SumChildren(m), // a[i] costs 1; a.b is free
            CallExpression => SumChildren(node),         // calls are free (arguments still count)
            NewExpression => SumChildren(node),
            SequenceExpression => SumChildren(node),
            ObjectExpression => SumChildren(node),
            ArrayExpression => SumChildren(node),
            SpreadElement s => Get(s.Argument),
            AwaitExpression a => Get(a.Argument),
            YieldExpression y => Get(y.Argument),
            ChainExpression c => Get(c.Expression),
            TaggedTemplateExpression t => Get(t.Tag),
            TemplateLiteral => 0,
            _ => SumChildren(node),                      // identifiers, literals, etc. -> 0
        };

        private int SumChildren(Node node)
        {
            var total = 0;
            foreach (var child in node.ChildNodes)
            {
                if (child != null) total += Get(child);
            }
            return total;
        }

        private sealed class RefComparer : IEqualityComparer<Node>
        {
            public static readonly RefComparer Instance = new();

            public bool Equals(Node? x, Node? y) => ReferenceEquals(x, y);

            public int GetHashCode(Node obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Paces a run to the game's tick rate: `ops * OpDuration` real seconds,
    /// batching ops like the interpreter's <=199-op execution steps
    /// (Execution.cs). Accounted ops always feed the counter; sleeping is
    /// skipped when <see cref="PacingEnabled"/> is false (headless/tests) or
    /// there is no sim.
    /// </summary>
    public sealed class TickPacer
    {
        /// <summary>Max ops per batch, mirroring the interpreter's step budget.</summary>
        private const long MaxBatchOps = 199;

        /// <summary>Cap a batch's real duration at ~half a second for smoothness.</summary>
        private const double BatchSeconds = 0.5;

        private readonly OpAccumulator _ops;
        private readonly IGleamRunController? _run;
        private readonly object _gate = new();
        private double _opDurationSeconds = 0.0025;
        private long _batch;
        private long _deadline; // GetTimestamp() units when the current budget is spent

        public TickPacer(OpAccumulator ops, IGleamRunController? run = null)
        {
            _ops = ops;
            _run = run;
        }

        /// <summary>Seconds per op at the current execution speed (default 2.5 ms = 1x).</summary>
        public double OpDurationSeconds
        {
            get => _opDurationSeconds;
            set => _opDurationSeconds = value > 0 ? value : 0.0025;
        }

        /// <summary>When false, ops are counted but never slept for.</summary>
        public bool PacingEnabled { get; set; } = true;

        /// <summary>Invoked on the executing thread with each paced batch (for sim side-effects).</summary>
        public Action<long>? OnBatch { get; set; }

        /// <summary>
        /// Account ops and (when pacing is enabled) sleep to stay at the tick rate.
        /// Thread-safe: drone engines share one pacer, and the batch lock makes all
        /// engines yield together once the shared budget is spent (like the game's
        /// interpreter stepping every state then yielding).
        /// </summary>
        public void Account(long ops)
        {
            if (ops <= 0) return;
            _ops.Add(ops);
            if (!PacingEnabled) return;

            lock (_gate)
            {
                _batch += ops;
                if (_batch < BatchThreshold) return;
                var batch = _batch;
                _batch = 0;
                Pace(batch);
                OnBatch?.Invoke(batch);
            }
        }

        private long BatchThreshold
        {
            get
            {
                var byTime = (long)(BatchSeconds / _opDurationSeconds);
                return Math.Max(1, Math.Min(MaxBatchOps, byTime));
            }
        }

        private void Pace(long batch)
        {
            var now = Stopwatch.GetTimestamp();
            var budget = (long)(batch * _opDurationSeconds * Stopwatch.Frequency);
            var end = _deadline + budget;
            if (end < now) end = now + budget; // running slow: don't burst to catch up
            _deadline = end;

            while (now < end)
            {
                if (_run != null && _run.IsStopped) throw new GleamStoppedException();
                var remainMs = (end - now) * 1000.0 / Stopwatch.Frequency;
                Thread.Sleep(Math.Max(1, Math.Min(50, (int)remainMs)));
                now = Stopwatch.GetTimestamp();
            }
        }
    }
}