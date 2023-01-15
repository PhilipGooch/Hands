using UnityEngine;

public class HandPositions : MonoBehaviour
{
    [SerializeField]
    Transform pointingTransform;

    public Vector3 PointingPosition => pointingTransform.position;
}
