using UnityEngine.Scripting;
[assembly: Preserve] // IL2CPP
[assembly: AlwaysLinkAssembly] // IL2CPP

namespace NBG.Core.DataMining.StandardSources
{
    public enum DataSourceId : byte
    {
        FrameTiming = 10,
        SceneEvent = 11,
        StandardCounters = 12,
        FixedUpdate = 13,

        BeginUserId = 32
    }
}
