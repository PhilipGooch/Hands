using NBG.Core;
using UnityEngine;

public class DontDestroyThis : MonoBehaviour
{
    void Start()
    {
        if (this.transform.parent != null)
        {
            Debug.LogWarning($"DontDestroyThis only works on root objects: {this.gameObject.GetFullPath()}", this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
}
