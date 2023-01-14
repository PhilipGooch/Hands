//#define PLUGIN_IS_STATICALLY_LINKED

using System.Runtime.InteropServices;

namespace NBG.RePhysX
{
    public static class Plugin
    {
#if PLUGIN_IS_STATICALLY_LINKED
        const string plugin = "__Internal";
#elif UNITY_SWITCH && !UNITY_EDITOR
        const string plugin = "libRePhysX";
#else
        const string plugin = "RePhysX";
#endif

        public delegate void DebugLogDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string message
            );
        
        [DllImport(plugin)]
        public static extern bool Initialize(DebugLogDelegate debugLogPtr, int maxThreads = 2, int solvertype = 0);
        
        [DllImport(plugin)]
        public static extern void Shutdown();
    }
}
