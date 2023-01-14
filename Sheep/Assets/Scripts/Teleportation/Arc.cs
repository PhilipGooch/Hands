using UnityEngine;

public static class Arc
{
    public static Vector3 GetArcPositionAtTime(float time, Vector3 startPosition, Vector3 initialVelocity)
    {
        return startPosition + ((initialVelocity * time) + (0.5f * time * time) * Physics.gravity);
    }

    public static float GetArcTimeAtHeight(float height, Vector3 startPosition, Vector3 initialVelocity)
    {
        float s = startPosition.y - height;
        float u = initialVelocity.y;
        float a = -Physics.gravity.y;
        float t = (u + Mathf.Sqrt(Mathf.Pow(u, 2) + 2 * a * s)) / a;
        return t;
    }
}
