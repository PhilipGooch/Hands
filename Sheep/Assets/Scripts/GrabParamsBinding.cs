
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabParamsBinding : MonoBehaviour
{
    public GrabParams grabParams;
    [SerializeField]
    float priority = 0f;

    public float Priority => priority;

    public bool Grabbable { get; set; } = true;

    public static GrabParams GetParams(GameObject gameobject)
    {
        var gpb = gameobject.GetComponentInParent<GrabParamsBinding>();
        if (gpb != null && gpb.grabParams != null)
            return gpb.grabParams;
        else
            return GrabParams.defaults;
    }
}

