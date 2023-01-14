using AOT;
using UnityEngine;

namespace CoreSample.RePhysX
{
    public class RePhysXPluginSample : MonoBehaviour
    {
        void Awake()
        {
            if (!NBG.RePhysX.Plugin.Initialize(OnDebugLog))
                throw new System.Exception();
        }

        void OnDestroy()
        {
            NBG.RePhysX.Plugin.Shutdown();
        }

        [MonoPInvokeCallback(typeof(NBG.RePhysX.Plugin.DebugLogDelegate))]
        static void OnDebugLog(string message)
        {
            Debug.Log($"[RePhysX:n] {message}");
        }
    }
}
