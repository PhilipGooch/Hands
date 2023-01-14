using System.Threading;
using UnityEngine;

namespace NBG.Core
{
    public static class ThreadUtilities
    {
        [ClearOnReload]
        static Thread s_MainThread;
        public static Thread MainThread => s_MainThread;

        [RuntimeInitializeOnLoadMethod]
        static void StoreMainThread()
        {
            Debug.Assert(s_MainThread == null);
            s_MainThread = Thread.CurrentThread;
        }
    }
}
