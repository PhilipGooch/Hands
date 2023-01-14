using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolableWarmup : MonoBehaviour
{
    [System.Serializable]
    public struct WarmupConfiguration
    {
        [Tooltip("Target poolable to warm up.")]
        public Poolable target;
        [Tooltip("Number of poolable instances to create.")]
        public int count;
    }

    [SerializeField]
    List<WarmupConfiguration> warmupConfigurations = new List<WarmupConfiguration>();
    [SerializeField]
    WarmupConfiguration test;

    private void Start()
    {
        // Prevent poolables from playing audio while warming up
        AudioManager.instance.SetAudioActive(false);
        foreach(var config in warmupConfigurations)
        {
            if (config.target)
            {
                config.target.WarmupPool(config.count);
            }
        }
        AudioManager.instance.SetAudioActive(true);
    }
}
