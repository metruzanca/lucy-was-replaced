using System;

namespace GleamRuntime
{
    /// <summary>Thrown to unwind a paced run when the player stops it.</summary>
    public sealed class GleamStoppedException : Exception
    {
        public GleamStoppedException() : base("Program stopped by the player.") { }
    }

    /// <summary>Lets the game bridge abort pacing waits when the run is stopped.</summary>
    public interface IGleamRunController
    {
        bool IsStopped { get; }
    }

    /// <summary>Handles `io.println` output (in-game: print bubble + pacing; headless: log).</summary>
    public interface IGleamPrintHandler
    {
        void Print(string text);
    }
}