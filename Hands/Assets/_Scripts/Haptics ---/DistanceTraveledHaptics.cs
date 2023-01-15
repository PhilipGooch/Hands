using UnityEngine;

public class DistanceTraveledHaptics : HapticsBase
{
    [SerializeField]
    private float hapticsInterval = 0.1f;
    [Tooltip("override what is treated as this objects world space")]
    [SerializeField]
    private Transform relativeTransform;

    private float sqrDistanceGoal => hapticsInterval * hapticsInterval;

    private Vector3 lastHapticPos;

    protected override void Start()
    {
        base.Start();
        lastHapticPos = GetPosition();
    }

    private void FixedUpdate()
    {
        var newPos = GetPosition();

        if (Vector3.SqrMagnitude(lastHapticPos - newPos) >= sqrDistanceGoal)
        {
            TryVibrate();
            lastHapticPos = newPos;
        }
    }

    Vector3 GetPosition()
    {
        return relativeTransform != null ? relativeTransform.InverseTransformPoint(transform.position) : transform.position; 
    }
}
