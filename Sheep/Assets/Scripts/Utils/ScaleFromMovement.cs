using UnityEngine;

public class ScaleFromMovement : MonoBehaviour
{
    [SerializeField]
    Vector3 axis = Vector3.up;

    Vector3 startingPosition;
    Vector3 baseScale;
    Vector3 size;
    float fullLength;

    private void Start()
    {
        axis = axis.normalized;
        startingPosition = transform.position;
        size = Vector3.Project(GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size, axis);
        fullLength = size.magnitude;
        var scaleAlongAxis = Vector3.Project(transform.localScale, axis);
        var nonUniformScaleModifier = axis - scaleAlongAxis;
        baseScale = transform.localScale - scaleAlongAxis - nonUniformScaleModifier;
    }

    private void Update()
    {
        var positionDiff = transform.position - startingPosition;
        positionDiff = Quaternion.Inverse(transform.rotation) * positionDiff;
        var axisAlignedDiff = Vector3.Project(positionDiff, axis);
        var diffLength = axisAlignedDiff.magnitude;

        var scaleFromLength = (fullLength - diffLength) / fullLength;

        transform.localScale = baseScale + axis * scaleFromLength;
    }
}
