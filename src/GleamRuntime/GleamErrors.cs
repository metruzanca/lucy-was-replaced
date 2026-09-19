using System;
using Jint.Runtime;

namespace GleamRuntime
{
    /// <summary>
    /// Turns raw runtime exceptions into player-facing messages. Keeps Jint's
    /// internals (wrapper exceptions, .NET stack traces) out of the code window.
    /// </summary>
    public static class GleamErrors
    {
        private const string TodoMarker = "`todo` expression evaluated";

        /// <summary>
        /// True when the exception (or its inner chain) is the Gleam `todo`
        /// placeholder — an empty function body that was compiled and executed.
        /// </summary>
        public static bool IsTodo(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is JavaScriptException jse &&
                    jse.Message.IndexOf(TodoMarker, StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// A player-facing description of a runtime failure. The Gleam `todo`
        /// placeholder gets a friendly explanation with the JS call site; every
        /// other error falls back to its raw text.
        /// </summary>
        public static string Describe(Exception ex)
        {
            var real = Unwrap(ex);
            if (real is JavaScriptException && IsTodo(real))
            {
                var trace = ((JavaScriptException)real).GetJavaScriptErrorString();
                return
                    "This program ran into an unfinished function. Gleam compiles an empty " +
                    "function body to a `todo` placeholder, and executing it throws.\n\n" +
                    trace +
                    "\n\nFill in the empty function body (or remove the call) and run again.";
            }
            return real.ToString();
        }

        private static Exception Unwrap(Exception ex)
        {
            while (ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
                ex = aggregate.InnerExceptions[0];
            return ex;
        }
    }
}