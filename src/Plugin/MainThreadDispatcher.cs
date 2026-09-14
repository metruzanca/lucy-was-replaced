using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GleamFarmer
{
    /// <summary>
    /// Runs actions on the Unity main thread, pumped from the plugin's Update().
    /// Callers (the Gleam worker thread) block until the action completes.
    /// </summary>
    public sealed class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<(Func<object?> Work, TaskCompletionSource<object?> Done)> _queue = new();

        public T Invoke<T>(Func<T> work)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue((() => work()!, completion));
            completion.Task.Wait();
            return (T)completion.Task.Result!;
        }

        /// <summary>Drain pending actions. Must be called on the Unity main thread.</summary>
        public void Pump()
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    item.Done.SetResult(item.Work());
                }
                catch (Exception ex)
                {
                    item.Done.SetException(ex);
                }
            }
        }
    }
}