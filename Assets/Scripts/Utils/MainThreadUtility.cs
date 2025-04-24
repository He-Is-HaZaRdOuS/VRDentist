using System;
using System.Threading;
using UnityEngine;

namespace Utils
{
    public static class MainThreadUtility
    {
        private static SynchronizationContext _mainThreadContext;

        static MainThreadUtility()
        {
            // Capture the SynchronizationContext of the main thread (Unity's main thread).
            _mainThreadContext = SynchronizationContext.Current;
        }

        // Executes the given action on the main thread.
        public static void ExecuteOnMainThread(Action action)
        {
            // Dispatch the action to the main thread's context.
            _mainThreadContext.Post(_ => action(), null);
        }
    }
}
