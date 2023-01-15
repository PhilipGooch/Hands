using System.Collections;
using UnityEngine;
using VR.System;

public class PlayArea : MonoBehaviour
{
    public static float scale = 12;
    public Vector3 center = new Vector3(0, 0, 0);
    public Vector3 size = new Vector3(36, 20, 27);
    [SerializeField]
    bool movePlayerToCenter = false;

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 0, 1, .25f);
        Gizmos.DrawCube(center,size);
        Gizmos.color = new Color(0, 0, 1, 1);
        Gizmos.DrawWireCube(center, size);
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => VRSystem.Instance.Initialized);
        if (size != new Vector3(36, 20, 27))
        {
            Debug.Log("Play area should be 36x27 to give 12x scaling.");
            size = new Vector3(36, 20, 27); // 12x scaling
        }

        // SteamVR basically returns this after parsing an enum as a string
        // So just mimic this behaviour for every platform
        var x = 3f;
        var z = 2.25f;

        if (Mathf.Approximately(x, 0) || Mathf.Approximately(z, 0))
            yield break;

        var vrArea = VRSystem.Instance.GetVRParent();
        if (!vrArea)
            yield break;

        //Debug.LogFormat("{0} {1}",x,z);
        scale = Mathf.Max( size.x/x, size.z/z);
        vrArea.localScale = Vector3.one * scale;
        if (movePlayerToCenter)
        {
            vrArea.position = transform.TransformPoint(center + Vector3.down * size.y / 2);
        }
    }
}
