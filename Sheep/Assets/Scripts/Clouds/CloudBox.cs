using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;

public class CloudBox : MonoBehaviour
{
    public float fadeInDuration = 0;
    public float fadeInTime = 0;
    public float fade = 1;
    public Vector3 innerSize = new Vector3(100, 50, 100);
    public Vector3 outerSize = new Vector3(120, 70, 120);

    public static List<CloudBox> all = new List<CloudBox>();
    public static System.Object cloudLock = new System.Object();

    public CloudBoxData cloudBoxData;
    //public static CloudBox main;

    void OnEnable()
    {
        lock (cloudLock)
        {
            all.Add(this);
        }

        cloudBoxData = new CloudBoxData()
        {
            pos = transform.position,
            innerSize = innerSize,
            outerSize = outerSize,
            fade = fade
        };
    }

    void OnDisable()
    {
        lock (cloudLock)
        {
            all.Remove(this);
        }
    }

    // saves 0.5ms per 4K
    Vector3 transformPosition;
    public void ReadPos()
    {
        transformPosition = transform.position;
    }

    [BurstCompatible]
    public struct CloudBoxData
    {
        public float3 pos;
        public float3 innerSize;
        public float3 outerSize;
        public float fade;
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, innerSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, outerSize);
    }

    void Update()
    {
        if (fadeInDuration == 0)
        {
            fade = 1;
        }
        else
        {
            fade = Mathf.Clamp01(fadeInTime / fadeInDuration);
            fadeInTime += Time.deltaTime;
         
        }

        cloudBoxData.fade = fade;
        cloudBoxData.pos = transformPosition;
    }

    public void FadeIn(float duration)
    {
        fadeInTime = 0;
        fadeInDuration = duration;
    }

}
