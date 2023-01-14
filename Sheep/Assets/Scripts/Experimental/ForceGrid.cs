using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceGrid : MonoBehaviour
{
    public float width = 3;
    public float length = 3;
    public float spacing = .25f;
    public float forceScale = .2f;

    Mesh mesh;
    MeshFilter filter;
    Vector3[] meshVerts;
    MeshRenderer[] arrowRender;
    Transform[] arrows;
    public void Build(System.Func<Vector3, Vector3> fnSample)
    {
        int XX = Mathf.RoundToInt(width / spacing) + 1;
        int ZZ = Mathf.RoundToInt(length / spacing) + 1;
        EnsureDimension(XX, ZZ, spacing);
        for (int x = 0; x < XX; x++)
            for (int z = 0; z < ZZ; z++)
            {
                var localPos = new Vector3(x * spacing - width / 2, 0, z * spacing - length/2);
                var worldPos = transform.TransformPoint(localPos);
                var idx = x + XX * z;

                var force = fnSample(worldPos) * forceScale;
                arrows[idx].rotation = force.magnitude > 0.001f ? Quaternion.LookRotation(force) : Quaternion.identity;
                arrows[idx].localScale = new Vector3(spacing * .05f, spacing * .05f, force.magnitude);
                arrows[idx].position = worldPos + force / 2;
            }
    }

    private void EnsureDimension(int XX, int ZZ, float spacing)
    {
        if (arrows == null || arrows.Length != XX * ZZ)
        {
            if (arrows != null)
            {
                for (int i = 0; i < arrows.Length; i++)
                    Destroy(arrows[i].gameObject);
            }
            arrows = new Transform[XX * ZZ];
            arrowRender = new MeshRenderer[XX * ZZ];
            for (int x = 0; x < XX; x++)
                for (int z = 0; z < ZZ; z++)
                {
                    var localPos = new Vector3(x * spacing - width / 2, 0, z * spacing - length/2);
                    var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    DestroyImmediate(arrow.GetComponent<Collider>());
                    arrow.SetParent(transform, false);
                    arrow.localPosition = localPos;
                    arrows[x + XX * z] = arrow;
                    arrowRender[x + XX * z] = arrow.GetComponent<MeshRenderer>();
                }
        }
    }

    public static ForceGrid Create(Transform transform)
    {
        var go = new GameObject("forcegrid");
        go.transform.SetParent(transform, false);
        
        return go.AddComponent<ForceGrid>();
    }
}


