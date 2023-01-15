using UnityEngine;

public static class SheepMath3d
{
    public static Vector3 ProjectPointOnSegment(Vector3 linePoint, Vector3 lineVec, Vector3 point)
    {
        //get vector from point on line to point in space
        Vector3 linePointToPoint = point - linePoint;

        var t = Vector3.Dot(linePointToPoint, lineVec) / lineVec.sqrMagnitude;
        if (t <= 0) return linePoint;
        if (t >= 1) return linePoint + lineVec;
        return linePoint + lineVec * t;
    }

    // clamps to plane
    public static Vector3 ClampToPlane(Vector3 vector, Vector3 normal)
    {
        var dot = Vector3.Dot(vector, normal);
        if (dot > 0)
            return vector - dot * vector.normalized;
        return vector;
    }

    public static Quaternion EnsureValid(this Quaternion q)
    {
        if (q.w < -0f)
        {
            //Debug.LogError("Caught strange quaternion");
            q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
        }
        return q.normalized;
    }

    public static Vector3 VectorFromAngle(float angleRad, Vector3 forward, Vector3 up)
    {
        var localVector = new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
        var rotation = Quaternion.LookRotation(forward, up);
        return (rotation * localVector).normalized;
    }
}

