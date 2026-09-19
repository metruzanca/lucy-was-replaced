using System.Collections.Generic;
using System.Linq;

namespace GleamFarmer
{
    /// <summary>
    /// A stand-in Python AST <see cref="Node"/> used only to drive the game's
    /// <c>BlinkManager</c> line-highlight overlay from a Gleam run. The game draws a
    /// fading rect over <c>boxedParams.wordStart..wordEnd</c> in the code window and, in
    /// step-by-step mode, a persistent full-line rect for the latest node — exactly the
    /// visual the Gleam runtime needs, with no Unity math of our own.
    /// </summary>
    public sealed class GleamBlinkNode : Node
    {
        public GleamBlinkNode(CodeWindow codeWindow, int wordStart, int wordEnd)
            : base(codeWindow, wordStart, wordEnd)
        {
        }

        public override System.Collections.Generic.IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
            => System.Linq.Enumerable.Empty<double>();

        public override Node DeepCopy(Dictionary<object, object> copies) => this;
    }
}