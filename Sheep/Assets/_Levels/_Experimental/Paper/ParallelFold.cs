using NBG.Core;
using UnityEngine;

[ExecuteInEditMode]
public class ParallelFold : Fold
{
    [SerializeField]
    [HideInInspector]
    Transform left, right;

    [SerializeField]
    public bool showMesh, showFold, showMaths;

    [SerializeField]
    [Range(0, 4)]
    float leftOffset = 2, rightOffset = 2, leftRadius = 2, centerOffset = 0, depth = 1;
    float rightRadius = 2;
    Vector3 leftPoint, rightPoint, centerPoint;
    Vector3 projectionPoint;
    Vector3 intersectionPoint;

    protected override void OnEnable()
    {
        base.OnEnable();
        CalculateVertices();
        UpdateMesh();
    }

    void LateUpdate() // <--- need to update the transforms recursively.
    {
        if (Error()) return;

        CalculateVertices();
        SetVertices();
        rightOffset = Mathf.Clamp(rightOffset, 0, leftOffset + leftRadius);
        rightRadius = leftOffset + leftRadius - rightOffset;
        leftPoint = leftPivot.TransformPoint(new Vector3(-leftOffset, centerOffset, 0));
        rightPoint = rightPivot.TransformPoint(new Vector3(rightOffset, centerOffset, 0));
        centerPoint = leftPivot.TransformPoint(new Vector3(0, centerOffset, 0));
        CircleCircleIntersection(leftPoint, rightPoint, leftRadius, rightRadius);
        left.localPosition = leftPoint;
        right.localPosition = rightPoint;
        float leftAngle = Vector3.Angle(intersectionPoint - leftPoint, centerPoint - leftPoint);
        float rightAngle = -Vector3.Angle(intersectionPoint - rightPoint, centerPoint - rightPoint);
        left.rotation = Quaternion.AngleAxis(leftAngle, leftPivot.up) * leftPivot.rotation;
        right.rotation = Quaternion.AngleAxis(rightAngle, leftPivot.up) * rightPivot.rotation;
        ShowMesh(showMesh);
    }

    // https://mathworld.wolfram.com/Circle-CircleIntersection.html
    Vector3 CircleCircleIntersection(Vector3 p1, Vector3 p2, float r1, float r2)
    {
        float d = (p2 - p1).magnitude;
        float x = (Mathf.Pow(d, 2) + Mathf.Pow(r2, 2) - Mathf.Pow(r1, 2)) / (2 * d);
        float y = (1 / (2 * d)) * Mathf.Sqrt((-d + r1 - r2) * (-d - r1 + r2) * (-d + r1 + r2) * (d + r1 + r2));
        projectionPoint = p2 + ((p1 - p2).normalized * x);
        intersectionPoint = projectionPoint + Vector3.Cross((p1 - p2).normalized, leftPivot.up) * y;
        return intersectionPoint;
    }

    void OnDrawGizmos()
    {
        if (showMaths)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(centerPoint, leftPoint);
            Gizmos.DrawLine(centerPoint, rightPoint);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rightPoint, projectionPoint);
            Gizmos.DrawLine(projectionPoint, intersectionPoint);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftPoint, intersectionPoint);
            Gizmos.DrawLine(rightPoint, intersectionPoint);
            DebugExtension.DrawCircle(leftPoint, leftPivot.up, Color.black, leftRadius);
            DebugExtension.DrawCircle(rightPoint, rightPivot.up, Color.black, rightRadius);
        }
    }

    void CalculateVertices()
    {
        leftVertices.Clear();
        rightVertices.Clear();
        leftVertices.Add(new Vector3(0, depth));
        leftVertices.Add(new Vector3(0, 0));
        leftVertices.Add(new Vector3(leftRadius, 0));
        leftVertices.Add(new Vector3(leftRadius, depth));
        rightVertices.Add(new Vector3(-rightRadius, depth));
        rightVertices.Add(new Vector3(-rightRadius, 0));
        rightVertices.Add(new Vector3(0, 0));
        rightVertices.Add(new Vector3(0, depth));
    }
}
