using UnityEngine;

public class TwoAnchorRope : MonoBehaviour
{
    [SerializeField]
    Transform bottomAnchor;
    [SerializeField]
    Transform topAnchor;
    [SerializeField]
    Vector3 axis = Vector3.up;
    [SerializeField]
    float objectLength = 1f;

    Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        transform.position = bottomAnchor.position;

        var baseScale = Vector3.Scale(originalScale, Vector3.one - axis);
        var transformedScale = Vector3.Scale(originalScale, axis);

        var anchorDistance = (topAnchor.position - bottomAnchor.position).magnitude;
        transformedScale = transformedScale * anchorDistance / objectLength;
        transform.localScale = baseScale + transformedScale;

        Vector3 dir = topAnchor.position - bottomAnchor.position;

        var rotDiff = Quaternion.FromToRotation(Vector3.forward, axis);
        var localRot = transform.rotation * rotDiff;
        var calcRot = Quaternion.LookRotation(dir, localRot * axis);
        transform.rotation = calcRot * Quaternion.Inverse(rotDiff);
    }
}
