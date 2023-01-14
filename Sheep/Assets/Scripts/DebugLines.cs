using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLines : MonoBehaviour
{
    static DebugLines instance;
    int maxCount = 128;

    Transform[] arrows;
    MeshRenderer[] arrowRender;

    int currentCount = 0;

    private void Awake()
    {
        instance = this;
        EnsureDimension(maxCount);
    }
    public void FixedUpdate()
    {
        
        for (int i = currentCount; i < maxCount; i++)
            arrows[i].gameObject.SetActive(false);
        currentCount = 0;
    }

    public static void DrawRay(Vector3 from, Vector3 dir)
    {
        var l = Create();
        var idx = Mathf.Min(l.currentCount++, l.maxCount - 1);

        l.arrows[idx].rotation = dir.magnitude > 0.001f ? Quaternion.LookRotation(dir) : Quaternion.identity;
        l.arrows[idx].localScale = new Vector3(.02f, .02f, dir.magnitude);
        l.arrows[idx].position = from + dir / 2;
        l.arrows[idx].gameObject.SetActive(true);

    }
    public static void DrawLine(Vector3 from, Vector3 to)
    {
        DrawRay(from, to - from);
            
    }

    private void EnsureDimension(int maxCount)
    {
        if (arrows == null || arrows.Length != maxCount)
        {
            if (arrows != null)
            {
                for (int i = 0; i < arrows.Length; i++)
                    Destroy(arrows[i].gameObject);
            }
            arrows = new Transform[maxCount];
            arrowRender = new MeshRenderer[maxCount];
            for (int x = 0; x < maxCount; x++)
                {
                    //var localPos = new Vector3(x * spacing - width / 2, 0, z * spacing - length / 2);
                    var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    arrow.gameObject.layer = 2;// ignoreraycast
                    //DestroyImmediate(arrow.GetComponent<Collider>());
                    arrow.SetParent(transform, false);
                    //arrow.localPosition = localPos;
                    arrows[x] = arrow;
                    arrowRender[x] = arrow.GetComponent<MeshRenderer>();
                }
        }
    }

    public static DebugLines Create()
    {
        if (instance == null)
        {
            var go = new GameObject("debugLines");
            //go.transform.SetParent(transform, false);

            instance = go.AddComponent<DebugLines>();
        }
        return instance;
    }
}
