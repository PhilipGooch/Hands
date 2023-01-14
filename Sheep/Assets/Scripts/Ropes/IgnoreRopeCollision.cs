using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.XPBDRope;

public class IgnoreRopeCollision : MonoBehaviour
{
    [SerializeField]
    Rope targetRope;
    [SerializeField]
    Collider targetCollider;

    private void Start()
    {
        foreach(var bone in targetRope.Bones)
        {
            Physics.IgnoreCollision(bone.capsule, targetCollider);
        }
    }

    private void OnValidate()
    {
        if (!targetRope)
        {
            targetRope = GetComponent<Rope>();
        }
    }
}
