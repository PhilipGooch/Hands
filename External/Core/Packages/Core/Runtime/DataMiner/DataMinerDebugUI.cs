using System.Linq;
using UnityEngine;

namespace NBG.Core.DataMining
{
    internal class DataMinerDebugUI
    {
        const string kDebugCategory = "Data Miner";

        [ClearOnReload]
        static DebugUI.IDebugItem _registerItem;

        [RuntimeInitializeOnLoadMethod]
        static void RegisterDebugUI()
        {
            var debug = DebugUI.DebugUI.Get();
            _registerItem = debug.RegisterAction("Initialize", kDebugCategory, () => Initialize());
        }

        static void Initialize()
        {
            var debug = DebugUI.DebugUI.Get();
            debug.Unregister(_registerItem);
            _registerItem = null;

            debug.Print("[Data Miner] Initializing...");
            debug.RegisterBool("Is Recording?", kDebugCategory, () => DataMiner.IsRecording);
            debug.RegisterAction("Toggle recording", kDebugCategory, () => DataMiner.ToggleRecording());
            foreach (var source in DataMiner.Sources)
            {
                debug.RegisterBool($"Enable {source.GetType().Name}", kDebugCategory,
                    () => DataMiner.IsSourceEnabled(source),
                    (value) => DataMiner.EnableSource(source, value)
                    );
            }
            debug.Print($"[Data Miner] Registered {DataMiner.Sources.Count()} sources.");
        }
    }
}
