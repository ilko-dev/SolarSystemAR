using System;
using System.Collections.Concurrent;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Android calls us back on its own threads, but Unity objects may only be touched on Unity's main thread
    /// (like a web worker posting a message to the page). Callbacks are queued here and run by VoiceController.Update.
    /// </summary>
    public static class MainThreadQueue
    {
        private static readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();

        public static void Post(Action action)
        {
            if (action != null)
                Pending.Enqueue(action);
        }

        /// <summary>Run everything queued so far. Call from a MonoBehaviour's Update.</summary>
        public static void Drain()
        {
            while (Pending.TryDequeue(out Action action))
                action();
        }
    }
}
