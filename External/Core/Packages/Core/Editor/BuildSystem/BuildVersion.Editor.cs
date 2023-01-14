using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor
{
    class BuildVersion
    {
        [MenuItem("No Brakes Games/Automation/Build Version/Read Current (and print)")]
        static void DebugReadVersion()
        {
            var v = Core.BuildVersion.Instance;
            v.Refresh();
            Debug.Log(v);
        }
    }
}
