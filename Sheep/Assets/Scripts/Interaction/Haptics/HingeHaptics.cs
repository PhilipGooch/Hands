using UnityEngine;

public class HingeHaptics : HapticsBase
{
    [HideInInspector]
    [SerializeField]
    private HingeJoint joint;
    [SerializeField]
    private float hapticsAngleInterval;
    private float lastHapticAngle;

    private void OnValidate()
    {
        if (joint == null)
            joint = GetComponent<HingeJoint>();
    }

    private void FixedUpdate()
    {
        if (Mathf.Abs(joint.angle - lastHapticAngle) >= hapticsAngleInterval)
        {
            TryVibrate();
            lastHapticAngle = joint.angle;
        }
    }
}
