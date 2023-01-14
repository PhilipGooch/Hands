using NBG.XPBDRope;
using UnityEngine;

public class RopeLengthChangeHaptics : HapticsBase
{
    [HideInInspector]
    [SerializeField]
    private Rope rope;
    [SerializeField]
    private float hapticsInterval;
    private float lastHapticLength;

    private void OnValidate()
    {
        if (rope == null)
            rope = GetComponent<Rope>();
    }

    private void FixedUpdate()
    {
        var currLength = rope.CurrentRopeLength;
        if (Mathf.Abs(lastHapticLength - currLength) >= hapticsInterval)
        {
            TryVibrate();
            lastHapticLength = currLength;
        }
    }
}
