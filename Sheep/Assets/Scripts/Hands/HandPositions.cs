using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPositions : MonoBehaviour
{
    [SerializeField]
    Transform threatTransform;
    [SerializeField]
    Transform pointingTransform;
    [SerializeField]
    Collider indexCollider;
    [SerializeField]
    Collider fistCollider;
    [SerializeField]
    Collider handCollider;

    public Vector3 ThreatPosition => threatTransform.position;
    public Vector3 PointingPosition => pointingTransform.position;
    public Collider IndexCollider => indexCollider;
    public Collider FistCollider => fistCollider;
    public Collider HandCollider => handCollider;
}
